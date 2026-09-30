/*
 * UO Community Script: Apex Hunt - Automated Server-Wide PvM Hunting Competition
 * Original Author: Imagine (Resource #2678)
 * Source: https://www.servuo.dev/archive/release-apex-hunt-automated-server-wide-pvm-hunting-competition.2678/
 *
 * Automated server-wide hunting competition with tiered target creatures,
 * live HUD, leaderboard gump, tie-breaker mechanic, and bank-delivered gold bounties.
 *
 * Enhancements:
 * - Dynamic Triggering: Replaced hard-coded 4-hour interval with a minimum 2-hour
 *   interval between events, followed by an hourly 10% chance check requiring at least
 *   one player character online before scheduling the 10-minute warning countdown.
 */

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Server;
using Server.Commands;
using Server.Gumps;
using Server.Items;
using Server.Mobiles;
using Server.Network;
using Server.Regions;

namespace Server.Custom.ApexHunt
{
    public static class ApexHuntEvent
    {
        // --- Persistence Path ---
        private static readonly string SavePath = Path.Combine("Saves", "ApexHuntEvent.bin");
        private static readonly object _lock = new object();

        // --- State Variables ---
        private static bool _enabled = true;
        public static bool Enabled { get { return _enabled; } set { _enabled = value; } }

        private static bool _eventActive = false;
        public static bool IsEventActive => _eventActive;

        private static bool _eventEnding = false;
        public static bool IsEventEnding => _eventEnding;

        private static bool _eventScheduled = false;
        public static bool IsEventScheduled => _eventScheduled;

        private static Type _targetCreatureType;
        public static Type CurrentTargetCreature => _targetCreatureType;

        private static string _targetCreatureDisplayName;
        public static string CurrentTargetDisplayName => _targetCreatureDisplayName;

        private static int _targetKillCount;
        public static int CurrentTargetKillCount => _targetKillCount;

        private static DateTime _eventStartTime;
        public static DateTime EventStartTime => _eventStartTime;

        private static TimeSpan _eventDuration;
        public static TimeSpan EventDuration => _eventDuration;

        private static DateTime _nextCheckTime;
        public static DateTime NextCheckTime => _nextCheckTime;

        private static DateTime _nextEventTime;
        public static DateTime NextEventTime
        {
            get
            {
                if (_eventScheduled)
                    return _nextEventTime;
                return _nextCheckTime;
            }
        }

        private static Serial _firstGoalAchiever = Serial.Zero;

        // Player Kills: Serial -> Kill Count
        private static readonly Dictionary<Serial, int> _playerProgress = new Dictionary<Serial, int>();

        // Pending Rewards for offline players
        public enum RewardType { FirstPlace, SecondPlace, ThirdPlace, Participation }
        private static readonly Dictionary<Serial, List<RewardType>> _pendingRewards = new Dictionary<Serial, List<RewardType>>();

        // Warnings sent tracker
        private static readonly Dictionary<TimeSpan, bool> _warningsSent = new Dictionary<TimeSpan, bool>();

        // Server tick timer
        private static Timer _cycleTimer;

        public static TimeSpan RemainingTime
        {
            get
            {
                if (!_eventActive)
                    return TimeSpan.Zero;

                DateTime endTime = _eventEnding 
                    ? (_eventStartTime + _eventDuration + ApexHuntConfig.TieBreakerDuration) 
                    : (_eventStartTime + _eventDuration);

                TimeSpan remaining = endTime - DateTime.UtcNow;
                return remaining.TotalSeconds > 0 ? remaining : TimeSpan.Zero;
            }
        }

        // --- Initialization ---
        public static void Initialize()
        {
            EventSink.WorldSave += OnWorldSave;
            EventSink.CreatureDeath += OnCreatureDeath;
            EventSink.Login += OnLogin;

            // Primary Apex Hunt Commands
            CommandSystem.Register("ApexHuntStart", AccessLevel.Administrator, ApexHuntStart_OnCommand);
            CommandSystem.Register("ApexHuntStop", AccessLevel.Administrator, ApexHuntStop_OnCommand);
            CommandSystem.Register("ApexHuntStatus", AccessLevel.Player, ApexHuntStatus_OnCommand);
            CommandSystem.Register("ApexHunt", AccessLevel.Player, ApexHuntStatus_OnCommand);
            CommandSystem.Register("ApexHuntToggle", AccessLevel.Player, ApexHuntToggle_OnCommand);
            CommandSystem.Register("ApexHuntTop", AccessLevel.Player, ApexHuntTop_OnCommand);

            // Backward compatibility aliases
            CommandSystem.Register("PvMRushStart", AccessLevel.Administrator, ApexHuntStart_OnCommand);
            CommandSystem.Register("PvMRushStop", AccessLevel.Administrator, ApexHuntStop_OnCommand);
            CommandSystem.Register("PvMRushStatus", AccessLevel.Player, ApexHuntStatus_OnCommand);
            CommandSystem.Register("PvMRushToggle", AccessLevel.Player, ApexHuntToggle_OnCommand);
            CommandSystem.Register("PvMRushTop", AccessLevel.Player, ApexHuntTop_OnCommand);

            Load();

            // Start synchronized ServUO timer
            _cycleTimer = Timer.DelayCall(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(1), OnCycleTick);
        }

        // --- Timer Tick (Executes on ServUO main thread) ---
        private static void OnCycleTick()
        {
            lock (_lock)
            {
                DateTime now = DateTime.UtcNow;

                if (!_enabled)
                {
                    if (_eventActive)
                        EndEvent(true);
                    else if (_eventScheduled)
                    {
                        _eventScheduled = false;
                        _warningsSent.Clear();
                    }
                    return;
                }

                if (_eventActive)
                {
                    DateTime eventEndTime = _eventStartTime + _eventDuration;
                    if (_eventEnding)
                        eventEndTime += ApexHuntConfig.TieBreakerDuration;

                    TimeSpan timeRemaining = eventEndTime - now;

                    // Warning announcements
                    if (!_eventEnding)
                    {
                        if (timeRemaining <= ApexHuntConfig.PostEventWarning1 && timeRemaining > ApexHuntConfig.PostEventWarning2 && !WasWarningSent(ApexHuntConfig.PostEventWarning1))
                        {
                            BroadcastMessage(0x35, "[Apex Hunt] 5 minutes remaining.");
                            SetWarningSent(ApexHuntConfig.PostEventWarning1, true);
                        }
                        else if (timeRemaining <= ApexHuntConfig.PostEventWarning2 && timeRemaining > ApexHuntConfig.PostEventWarning3 && !WasWarningSent(ApexHuntConfig.PostEventWarning2))
                        {
                            BroadcastMessage(0x35, "[Apex Hunt] 2 minutes remaining.");
                            SetWarningSent(ApexHuntConfig.PostEventWarning2, true);
                        }
                        else if (timeRemaining <= ApexHuntConfig.PostEventWarning3 && timeRemaining > TimeSpan.Zero && !WasWarningSent(ApexHuntConfig.PostEventWarning3))
                        {
                            BroadcastMessage(0x35, "[Apex Hunt] 30 seconds remaining.");
                            SetWarningSent(ApexHuntConfig.PostEventWarning3, true);
                        }
                    }

                    if (now >= eventEndTime)
                    {
                        EndEvent(false);
                    }
                    else
                    {
                        // Refresh open HUD gumps every 5 seconds
                        if (now.Second % 5 == 0)
                        {
                            RefreshActiveHudGumps();
                        }
                    }
                }
                else if (_eventScheduled)
                {
                    TimeSpan timeToNext = _nextEventTime - now;

                    if (timeToNext <= ApexHuntConfig.PreEventWarning1 && timeToNext > ApexHuntConfig.PreEventWarning2 && !WasWarningSent(ApexHuntConfig.PreEventWarning1))
                    {
                        BroadcastMessage(0x59, "[Apex Hunt] Event starts in 10 minutes.");
                        SetWarningSent(ApexHuntConfig.PreEventWarning1, true);
                    }
                    else if (timeToNext <= ApexHuntConfig.PreEventWarning2 && timeToNext > ApexHuntConfig.PreEventWarning3 && !WasWarningSent(ApexHuntConfig.PreEventWarning2))
                    {
                        BroadcastMessage(0x59, "[Apex Hunt] Event starts in 5 minutes.");
                        SetWarningSent(ApexHuntConfig.PreEventWarning2, true);
                    }
                    else if (timeToNext <= ApexHuntConfig.PreEventWarning3 && timeToNext > TimeSpan.Zero && !WasWarningSent(ApexHuntConfig.PreEventWarning3))
                    {
                        BroadcastMessage(0x59, "[Apex Hunt] Event starts in 1 minute.");
                        SetWarningSent(ApexHuntConfig.PreEventWarning3, true);
                    }
                    else if (now >= _nextEventTime)
                    {
                        _eventScheduled = false;
                        StartEvent();
                    }
                }
                else
                {
                    // Minimum 2-hour interval & periodic check (10% chance if >= 1 player online)
                    if (now >= _nextCheckTime)
                    {
                        int onlineCount = GetOnlinePlayerCount();

                        TimeSpan retryDelay = ApexHuntConfig.GetRandomCheckInterval();

                        if (onlineCount >= ApexHuntConfig.MinOnlinePlayers)
                        {
                            if (Utility.RandomDouble() < ApexHuntConfig.TriggerChance)
                            {
                                _eventScheduled = true;
                                _nextEventTime = now + ApexHuntConfig.PreEventWarning1;
                                _warningsSent.Clear();
                                BroadcastMessage(0x59, "[Apex Hunt] Event starts in 10 minutes.");
                                SetWarningSent(ApexHuntConfig.PreEventWarning1, true);
                                Console.WriteLine($"[ApexHuntEvent] Trigger roll succeeded ({ApexHuntConfig.TriggerChance:P0}). Hunt starts in {ApexHuntConfig.PreEventWarning1.TotalMinutes}m with {onlineCount} online player(s).");
                                Save();
                                return;
                            }
                            else
                            {
                                Console.WriteLine($"[ApexHuntEvent] Trigger check: {onlineCount} player(s) online, {ApexHuntConfig.TriggerChance:P0} roll failed. Retrying in {retryDelay.TotalMinutes:F0}m.");
                            }
                        }
                        else
                        {
                            Console.WriteLine($"[ApexHuntEvent] Trigger check skipped: No players online. Retrying in {retryDelay.TotalMinutes:F0}m.");
                        }

                        _nextCheckTime = now + retryDelay;
                        Save();
                    }
                }
            }
        }

        // --- Start Event ---
        public static void StartEvent()
        {
            lock (_lock)
            {
                var targetEntry = ApexHuntConfig.SelectRandomTarget();
                if (targetEntry == null)
                {
                    Console.WriteLine("[ApexHuntEvent] No eligible creatures configured to start hunt.");
                    _eventScheduled = false;
                    _nextCheckTime = DateTime.UtcNow + ApexHuntConfig.MinEventInterval;
                    return;
                }

                _eventScheduled = false;
                _targetCreatureType = targetEntry.CreatureType;
                _targetCreatureDisplayName = targetEntry.DisplayName;
                _targetKillCount = Utility.RandomMinMax(targetEntry.MinKills, targetEntry.MaxKills);
                _eventStartTime = DateTime.UtcNow;

                int durationMinutes = Utility.RandomMinMax(ApexHuntConfig.MinDurationMinutes, ApexHuntConfig.MaxDurationMinutes);
                _eventDuration = TimeSpan.FromMinutes(durationMinutes);

                _eventActive = true;
                _eventEnding = false;
                _firstGoalAchiever = Serial.Zero;
                _playerProgress.Clear();
                _warningsSent.Clear();
                ApexHuntHudGump.MinimizedPlayers.Clear();
                ApexHuntHudGump.ClosedPlayers.Clear();

                BroadcastMessage(0x35, $"[Apex Hunt] Target: {_targetCreatureDisplayName} ({_targetKillCount} kills).");

                // Send HUD to all online players
                foreach (NetState state in NetState.Instances)
                {
                    if (state.Mobile is PlayerMobile pm && pm.NetState != null && !pm.Deleted)
                    {
                        pm.CloseGump(typeof(ApexHuntHudGump));
                        pm.SendGump(new ApexHuntHudGump(pm));
                    }
                }

                // Discord Notification
                SendDiscordNotification($"⚔️ **Exile Apex Hunt Started!** Target: **{_targetCreatureDisplayName}** | Goal: **{_targetKillCount}** kills | Duration: **{durationMinutes}** mins. First hunters to complete the cull win up to {ApexHuntConfig.RewardGoldFirstPlace:#,##0}gp!");

                Save();
            }
        }

        // --- End Event ---
        public static void EndEvent(bool cancelled)
        {
            lock (_lock)
            {
                if (!_eventActive)
                    return;

                _eventActive = false;
                _eventEnding = false;

                if (cancelled)
                {
                    BroadcastMessage(0x35, "[Apex Hunt] Event cancelled by an Administrator.");
                }
                else
                {
                    BroadcastMessage(0x35, "[Apex Hunt] The hunt has concluded.");

                    var sorted = _playerProgress.OrderByDescending(kvp => kvp.Value).ToList();
                    string winnerSummary = "";

                    if (sorted.Count == 0)
                    {
                        BroadcastMessage(0x35, "[Apex Hunt] No hunters claimed bounties.");
                    }
                    else
                    {
                        // 1st Place
                        if (sorted.Count >= 1)
                        {
                            var p1 = sorted[0];
                            Mobile m1 = World.FindMobile(p1.Key);
                            string name1 = GetMobileName(p1.Key);
                            BroadcastMessage(0x43, $"[Apex Hunt] 1st: {name1} ({p1.Value} kills) - {ApexHuntConfig.RewardGoldFirstPlace:#,##0}gp");
                            GiveReward(m1, p1.Key, RewardType.FirstPlace);
                            winnerSummary += $"1st: **{name1}** ({p1.Value} kills)\n";
                        }

                        // 2nd Place
                        if (sorted.Count >= 2)
                        {
                            var p2 = sorted[1];
                            Mobile m2 = World.FindMobile(p2.Key);
                            string name2 = GetMobileName(p2.Key);
                            BroadcastMessage(0x43, $"[Apex Hunt] 2nd: {name2} ({p2.Value} kills) - {ApexHuntConfig.RewardGoldSecondPlace:#,##0}gp");
                            GiveReward(m2, p2.Key, RewardType.SecondPlace);
                            winnerSummary += $"2nd: **{name2}** ({p2.Value} kills)\n";
                        }

                        // 3rd Place
                        if (sorted.Count >= 3)
                        {
                            var p3 = sorted[2];
                            Mobile m3 = World.FindMobile(p3.Key);
                            string name3 = GetMobileName(p3.Key);
                            BroadcastMessage(0x43, $"[Apex Hunt] 3rd: {name3} ({p3.Value} kills) - {ApexHuntConfig.RewardGoldThirdPlace:#,##0}gp");
                            GiveReward(m3, p3.Key, RewardType.ThirdPlace);
                            winnerSummary += $"3rd: **{name3}** ({p3.Value} kills)\n";
                        }

                        // Participation rewards
                        int minPartKills = Math.Max(1, (int)(_targetKillCount * ApexHuntConfig.ParticipationKillThreshold));
                        for (int i = 3; i < sorted.Count; i++)
                        {
                            var p = sorted[i];
                            if (p.Value >= minPartKills)
                            {
                                Mobile m = World.FindMobile(p.Key);
                                GiveReward(m, p.Key, RewardType.Participation);
                            }
                        }
                    }

                    // Discord End Announcement
                    if (!string.IsNullOrEmpty(winnerSummary))
                    {
                        SendDiscordNotification($"🏆 **Exile Apex Hunt Finished!**\nTarget was: **{_targetCreatureDisplayName}**\n\n**Victorious Hunters:**\n{winnerSummary}\nGold bounties deposited directly to the winners' bank vaults!");
                    }
                }

                // Close active HUDs
                foreach (NetState state in NetState.Instances)
                {
                    if (state.Mobile is PlayerMobile pm)
                    {
                        pm.CloseGump(typeof(ApexHuntHudGump));
                    }
                }

                ApexHuntHudGump.MinimizedPlayers.Clear();
                ApexHuntHudGump.ClosedPlayers.Clear();

                _nextCheckTime = DateTime.UtcNow + ApexHuntConfig.MinEventInterval;
                _nextEventTime = DateTime.MinValue;
                Save();
            }
        }

        // --- Creature Death Sink ---
        private static void OnCreatureDeath(CreatureDeathEventArgs e)
        {
            if (!_eventActive)
                return;

            BaseCreature dead = e.Creature as BaseCreature;
            if (dead == null)
                return;

            // ANTI-EXPLOIT: Ignore summoned, controlled, or bonded creatures
            if (dead.Summoned || dead.Controlled || dead.IsBonded)
                return;

            // Resolve real killer (direct player attack OR pet/squire/summon kill)
            Mobile killer = e.Creature.LastKiller as Mobile;
            PlayerMobile pm = killer as PlayerMobile;
            if (pm == null && killer is BaseCreature bc)
            {
                pm = bc.ControlMaster as PlayerMobile;
            }

            if (pm == null || pm.Deleted)
                return;

            // Check eligible facet
            if (!ApexHuntConfig.EligibleMaps.Contains(pm.Map))
                return;

            // Check guarded region
            if (pm.Region is GuardedRegion && !((GuardedRegion)pm.Region).Disabled)
            {
                pm.SendMessage(0x22, "[Apex Hunt] Kills inside guarded regions do not count towards the hunt!");
                return;
            }

            // Verify target creature type
            if (dead.GetType() == _targetCreatureType)
            {
                lock (_lock)
                {
                    _playerProgress.TryGetValue(pm.Serial, out int currentKills);
                    currentKills++;
                    _playerProgress[pm.Serial] = currentKills;

                    pm.SendMessage(0x3F, $"[Apex Hunt] Target slain! You have {currentKills} / {_targetKillCount} kills.");

                    // Check if player completed the kill goal
                    if (currentKills >= _targetKillCount)
                    {
                        if (_firstGoalAchiever == Serial.Zero)
                        {
                            _firstGoalAchiever = pm.Serial;
                            _eventEnding = true;
                            BroadcastMessage(0x35, $"[Apex Hunt] Goal reached by {pm.Name}. 60-second tie breaker active.");
                        }
                    }

                    // Automatically refresh gumps for the killer on valid kill
                    if (pm.HasGump(typeof(ApexHuntLeaderboardGump)))
                    {
                        ApexHuntLeaderboardGump.UpdateLeaderboard(pm);
                    }

                    if (pm.HasGump(typeof(ApexHuntHudGump)))
                    {
                        ApexHuntHudGump.UpdateHud(pm);
                    }
                    else if (!pm.HasGump(typeof(ApexHuntLeaderboardGump)) && !ApexHuntHudGump.ClosedPlayers.Contains(pm.Serial))
                    {
                        pm.SendGump(new ApexHuntHudGump(pm));
                    }
                }
            }
        }

        // --- Reward Delivery ---
        private static void GiveReward(Mobile m, Serial serial, RewardType type)
        {
            int goldAmount = 0;
            string title = "";

            switch (type)
            {
                case RewardType.FirstPlace:
                    goldAmount = ApexHuntConfig.RewardGoldFirstPlace;
                    title = "1st Place Winner";
                    break;
                case RewardType.SecondPlace:
                    goldAmount = ApexHuntConfig.RewardGoldSecondPlace;
                    title = "2nd Place";
                    break;
                case RewardType.ThirdPlace:
                    goldAmount = ApexHuntConfig.RewardGoldThirdPlace;
                    title = "3rd Place";
                    break;
                case RewardType.Participation:
                    goldAmount = ApexHuntConfig.RewardGoldParticipation;
                    title = "Active Participant";
                    break;
            }

            if (goldAmount <= 0)
                return;

            // Offline handling
            if (m == null || m.Deleted || m.BankBox == null)
            {
                if (!_pendingRewards.ContainsKey(serial))
                    _pendingRewards[serial] = new List<RewardType>();

                _pendingRewards[serial].Add(type);
                Console.WriteLine($"[ApexHuntEvent] Hunter {serial} offline. Queued {goldAmount}gp for {title}.");
                return;
            }

            // Deliver directly to bank with backpack fallback
            Item gold = new Gold(goldAmount);
            if (!m.BankBox.TryDropItem(m, gold, false))
            {
                m.AddToBackpack(gold);
                m.SendMessage(0x3F, $"[Apex Hunt] Reward ({title}): Bank full, {goldAmount:#,##0}gp placed in backpack.");
            }
            else
            {
                m.SendMessage(0x3F, $"[Apex Hunt] Reward ({title}): {goldAmount:#,##0}gp deposited into your Bank Box.");
            }
        }

        // --- Login Sink (Claim Pending Rewards) ---
        private static void OnLogin(LoginEventArgs e)
        {
            Mobile m = e.Mobile;
            if (m == null || m.Deleted)
                return;

            lock (_lock)
            {
                if (_pendingRewards.TryGetValue(m.Serial, out var rewards) && rewards.Count > 0)
                {
                    m.SendMessage(0x3F, "[Apex Hunt] Unclaimed bounties delivered:");
                    foreach (var reward in rewards)
                    {
                        GiveReward(m, m.Serial, reward);
                    }

                    _pendingRewards.Remove(m.Serial);
                    Save();
                }

                // If event active, display HUD
                if (_eventActive && !m.HasGump(typeof(ApexHuntHudGump)) && m is PlayerMobile pm && !ApexHuntHudGump.ClosedPlayers.Contains(pm.Serial))
                {
                    pm.SendGump(new ApexHuntHudGump(pm));
                }
            }
        }

        private static void OnWorldSave(WorldSaveEventArgs e)
        {
            Save();
        }

        // --- HUD Refresh Helper ---
        private static void RefreshActiveHudGumps()
        {
            foreach (NetState state in NetState.Instances)
            {
                if (state.Mobile is PlayerMobile pm && pm.NetState != null && !pm.Deleted)
                {
                    if (pm.HasGump(typeof(ApexHuntHudGump)))
                    {
                        ApexHuntHudGump.UpdateHud(pm);
                    }

                    if (pm.HasGump(typeof(ApexHuntLeaderboardGump)))
                    {
                        ApexHuntLeaderboardGump.UpdateLeaderboard(pm);
                    }
                }
            }
        }

        // --- Commands ---
        [Usage("ApexHuntStart")]
        [Description("Manually starts an Exile Apex Hunt event.")]
        private static void ApexHuntStart_OnCommand(CommandEventArgs e)
        {
            if (_eventActive)
            {
                e.Mobile.SendMessage(0x22, "[Apex Hunt] An Apex Hunt is already running!");
                return;
            }

            _eventScheduled = false;
            e.Mobile.SendMessage(0x3F, "[Apex Hunt] Commencing Apex Hunt...");
            StartEvent();
        }

        [Usage("ApexHuntStop")]
        [Description("Stops the active Exile Apex Hunt.")]
        private static void ApexHuntStop_OnCommand(CommandEventArgs e)
        {
            if (_eventActive)
            {
                e.Mobile.SendMessage(0x22, "[Apex Hunt] Stopping Apex Hunt.");
                EndEvent(true);
            }
            else if (_eventScheduled)
            {
                _eventScheduled = false;
                _nextCheckTime = DateTime.UtcNow + ApexHuntConfig.MinEventInterval;
                _warningsSent.Clear();
                BroadcastMessage(0x22, "[Apex Hunt] Scheduled event cancelled by an Administrator.");
                e.Mobile.SendMessage(0x22, "[Apex Hunt] Scheduled Apex Hunt countdown cancelled.");
            }
            else
            {
                e.Mobile.SendMessage(0x22, "[Apex Hunt] There is no active or scheduled hunt to stop.");
            }
        }

        [Usage("ApexHuntStatus")]
        [Description("Opens the Apex Hunt leaderboard and details.")]
        private static void ApexHuntStatus_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                pm.CloseGump(typeof(ApexHuntLeaderboardGump));
                pm.SendGump(new ApexHuntLeaderboardGump(pm));
            }
        }

        [Usage("ApexHuntToggle")]
        [Description("Toggles the floating Apex Hunt HUD widget.")]
        private static void ApexHuntToggle_OnCommand(CommandEventArgs e)
        {
            if (e.Mobile is PlayerMobile pm)
            {
                if (pm.HasGump(typeof(ApexHuntHudGump)))
                {
                    ApexHuntHudGump.ClosedPlayers.Add(pm.Serial);
                    pm.CloseGump(typeof(ApexHuntHudGump));
                    pm.SendMessage(0x3B2, "[Apex Hunt] HUD closed. Type [ApexHuntToggle to open again.");
                }
                else
                {
                    ApexHuntHudGump.ClosedPlayers.Remove(pm.Serial);
                    pm.SendGump(new ApexHuntHudGump(pm));
                    pm.SendMessage(0x3F, "[Apex Hunt] HUD opened.");
                }
            }
        }

        [Usage("ApexHuntTop")]
        [Description("Displays the top 3 hunters in the current Apex Hunt in chat.")]
        private static void ApexHuntTop_OnCommand(CommandEventArgs e)
        {
            if (!_eventActive)
            {
                e.Mobile.SendMessage(0x22, "[Apex Hunt] No active hunt at the moment.");
                return;
            }

            var topList = GetTopPlayers(3);
            e.Mobile.SendMessage(0x35, $"--- Apex Hunt Top Hunters ({_targetCreatureDisplayName}) ---");
            if (topList.Count == 0)
            {
                e.Mobile.SendMessage(0x3B2, "No kills registered yet.");
                return;
            }

            for (int i = 0; i < topList.Count; i++)
            {
                var entry = topList[i];
                e.Mobile.SendMessage(0x3F, $"#{i + 1}: {GetMobileName(entry.Key)} with {entry.Value} kills");
            }
        }

        // --- Helpers ---
        public static int GetOnlinePlayerCount()
        {
            int count = 0;
            foreach (NetState state in NetState.Instances)
            {
                if (state.Mobile is PlayerMobile pm && pm.NetState != null && !pm.Deleted)
                {
                    count++;
                }
            }
            return count;
        }

        public static int GetPlayerKills(Serial serial)
        {
            _playerProgress.TryGetValue(serial, out int kills);
            return kills;
        }

        public static KeyValuePair<Serial, int> GetLeader()
        {
            if (_playerProgress.Count == 0)
                return new KeyValuePair<Serial, int>(Serial.Zero, 0);

            return _playerProgress.OrderByDescending(kvp => kvp.Value).FirstOrDefault();
        }

        public static List<KeyValuePair<Serial, int>> GetTopPlayers(int count)
        {
            return _playerProgress.OrderByDescending(kvp => kvp.Value).Take(count).ToList();
        }

        public static string GetMobileName(Serial serial)
        {
            Mobile m = World.FindMobile(serial);
            if (m != null && !string.IsNullOrEmpty(m.Name))
                return m.Name;

            return "[Offline Hunter]";
        }

        public static string FormatCreatureName(string name)
        {
            if (string.IsNullOrEmpty(name))
                return "Monster";

            return Regex.Replace(name, "([a-z])([A-Z])", "$1 $2");
        }

        private static void BroadcastMessage(int hue, string message)
        {
            foreach (NetState state in NetState.Instances)
            {
                state.Mobile?.SendMessage(hue, message);
            }
        }

        private static bool WasWarningSent(TimeSpan warning)
        {
            return _warningsSent.ContainsKey(warning) && _warningsSent[warning];
        }

        private static void SetWarningSent(TimeSpan warning, bool sent)
        {
            _warningsSent[warning] = sent;
        }

        private static void SendDiscordNotification(string message)
        {
        }

        // --- Binary Persistence ---
        public static void Save()
        {
            lock (_lock)
            {
                try
                {
                    string dir = Path.GetDirectoryName(SavePath);
                    if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                        Directory.CreateDirectory(dir);

                    using (BinaryWriter writer = new BinaryWriter(File.Create(SavePath)))
                    {
                        writer.Write((int)3); // File Version

                        writer.Write(_enabled);
                        writer.Write(_eventActive);
                        writer.Write(_eventEnding);
                        writer.Write(_firstGoalAchiever.Value);

                        if (_eventActive && _targetCreatureType != null)
                        {
                            writer.Write(_targetCreatureType.FullName ?? "");
                            writer.Write(_targetCreatureDisplayName ?? "");
                            writer.Write(_targetKillCount);
                            writer.Write(_eventStartTime.ToBinary());
                            writer.Write(_eventDuration.TotalMinutes);
                        }
                        else
                        {
                            writer.Write("");
                            writer.Write("");
                            writer.Write((int)0);
                            writer.Write((long)0);
                            writer.Write((double)0);
                        }

                        writer.Write(_nextEventTime.ToBinary());

                        // Player Progress
                        writer.Write(_playerProgress.Count);
                        foreach (var kvp in _playerProgress)
                        {
                            writer.Write(kvp.Key.Value);
                            writer.Write(kvp.Value);
                        }

                        // Pending Rewards
                        writer.Write(_pendingRewards.Count);
                        foreach (var kvp in _pendingRewards)
                        {
                            writer.Write(kvp.Key.Value);
                            writer.Write(kvp.Value.Count);
                            foreach (var reward in kvp.Value)
                            {
                                writer.Write((int)reward);
                            }
                        }

                        // Version 3 fields
                        writer.Write(_eventScheduled);
                        writer.Write(_nextCheckTime.ToBinary());
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ApexHuntEvent] Save Error: {ex.Message}");
                }
            }
        }

        public static void Load()
        {
            if (!File.Exists(SavePath))
            {
                _nextCheckTime = DateTime.UtcNow + ApexHuntConfig.MinEventInterval;
                _nextEventTime = DateTime.MinValue;
                _eventScheduled = false;
                return;
            }

            lock (_lock)
            {
                try
                {
                    using (BinaryReader reader = new BinaryReader(File.OpenRead(SavePath)))
                    {
                        int version = reader.ReadInt32();

                        if (version >= 1)
                        {
                            _enabled = reader.ReadBoolean();
                            _eventActive = reader.ReadBoolean();
                            _eventEnding = reader.ReadBoolean();
                            _firstGoalAchiever = reader.ReadInt32();

                            string typeName = reader.ReadString();
                            _targetCreatureDisplayName = reader.ReadString();
                            _targetKillCount = reader.ReadInt32();

                            if (version == 2)
                            {
                                reader.ReadString(); // consume unused mapName from v2 if present
                            }

                            _eventStartTime = DateTime.FromBinary(reader.ReadInt64());
                            _eventDuration = TimeSpan.FromMinutes(reader.ReadDouble());

                            if (!string.IsNullOrEmpty(typeName))
                            {
                                _targetCreatureType = ScriptCompiler.FindTypeByName(typeName);
                            }

                            _nextEventTime = DateTime.FromBinary(reader.ReadInt64());

                            int progressCount = reader.ReadInt32();
                            _playerProgress.Clear();
                            for (int i = 0; i < progressCount; i++)
                            {
                                Serial serial = reader.ReadInt32();
                                int kills = reader.ReadInt32();
                                _playerProgress[serial] = kills;
                            }

                            int pendingCount = reader.ReadInt32();
                            _pendingRewards.Clear();
                            for (int i = 0; i < pendingCount; i++)
                            {
                                Serial s = reader.ReadInt32();
                                int count = reader.ReadInt32();
                                List<RewardType> rewards = new List<RewardType>();
                                for (int j = 0; j < count; j++)
                                {
                                    rewards.Add((RewardType)reader.ReadInt32());
                                }
                                _pendingRewards[s] = rewards;
                            }

                            if (version >= 3)
                            {
                                _eventScheduled = reader.ReadBoolean();
                                _nextCheckTime = DateTime.FromBinary(reader.ReadInt64());
                            }
                            else
                            {
                                _eventScheduled = false;
                                _nextCheckTime = _nextEventTime > DateTime.UtcNow 
                                    ? _nextEventTime 
                                    : DateTime.UtcNow + ApexHuntConfig.MinEventInterval;
                            }
                        }
                    }

                    if (!_eventActive && !_eventScheduled && _nextCheckTime < DateTime.UtcNow)
                    {
                        _nextCheckTime = DateTime.UtcNow;
                    }

                    if (_eventActive && _targetCreatureType == null)
                    {
                        _eventActive = false;
                        _playerProgress.Clear();
                        _nextCheckTime = DateTime.UtcNow + ApexHuntConfig.MinEventInterval;
                    }

                    Console.WriteLine("[ApexHuntEvent] System loaded successfully.");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ApexHuntEvent] Load Error: {ex.Message}");
                    _eventActive = false;
                    _eventScheduled = false;
                    _nextCheckTime = DateTime.UtcNow + ApexHuntConfig.MinEventInterval;
                }
            }
        }
    }
}
