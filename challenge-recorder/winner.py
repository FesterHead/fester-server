#!/usr/bin/env python3
"""
Immutable Winner Minting CLI for Post-Maintenance Challenges.

Evaluates the pre-configured winner strategy ('random', 'first', or 'nth') from
/app/data/challenges.json for a given maintenance day and locks the winning
record into the SQLite 'winners' table (UNIQUE per maintenance_day).

Once minted for a maintenance day, the winner record is immutable unless
explicitly cleared with reset.py during testing.

Usage inside container:
  docker exec challenge-recorder python winner.py 2026-11-03
"""

import hashlib
import json
import logging
import os
import secrets
import sqlite3
import sys
from datetime import datetime, timedelta, timezone
from logging.handlers import RotatingFileHandler

import httpx

DB_PATH = os.environ.get("DB_PATH", "/app/data/completions.db")
CHALLENGES_CONFIG_PATH = os.environ.get("CHALLENGES_CONFIG_PATH", "/app/data/challenges.json")
LOGS_DIR = os.environ.get("LOGS_DIR", "/app/data/logs")
LOG_FILE = os.path.join(LOGS_DIR, "challenge-recorder.log")
DRAND_LATEST_URL = "https://api.drand.sh/public/latest"
HST_TZ = timezone(timedelta(hours=-10), name="HST")

os.makedirs(LOGS_DIR, exist_ok=True)


class HSTFormatter(logging.Formatter):
    def formatTime(self, record, datefmt=None):
        dt = datetime.fromtimestamp(record.created, tz=HST_TZ)
        if datefmt:
            return dt.strftime(datefmt)
        return dt.strftime("%Y-%m-%d %H:%M:%S HST")


logger = logging.getLogger("challenge-recorder-winner")
logger.setLevel(logging.INFO)

if not logger.handlers:
    _formatter = HSTFormatter(
        fmt="[%(asctime)s] [%(levelname)s] %(message)s"
    )
    _file_handler = RotatingFileHandler(
        LOG_FILE,
        maxBytes=10 * 1024 * 1024,
        backupCount=5,
        encoding="utf-8",
    )
    _file_handler.setFormatter(_formatter)
    _file_handler.setLevel(logging.INFO)
    logger.addHandler(_file_handler)


def sha256_hex(message: str) -> str:
    return hashlib.sha256(message.encode("utf-8")).hexdigest()


def load_challenges() -> dict:
    if not os.path.exists(CHALLENGES_CONFIG_PATH):
        return {}
    with open(CHALLENGES_CONFIG_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)
        return data if isinstance(data, dict) else {}


def build_commitment_preimage(config: dict, maintenance_day: str) -> tuple[str, str, str]:
    strategy = str(config.get("strategy", "random")).strip().lower()
    salt = str(config.get("strategy_salt") or config.get("seed_prefix") or maintenance_day).strip()
    if strategy == "nth":
        winner_n = int(config.get("winner_n", 1))
        preimage = f"{salt}:day={maintenance_day}:strategy=nth:n={winner_n}"
    else:
        preimage = f"{salt}:day={maintenance_day}:strategy={strategy}"
    return strategy, preimage, sha256_hex(preimage)


def fetch_drand_beacon() -> tuple[int, str]:
    try:
        with httpx.Client(timeout=10.0) as client:
            resp = client.get(DRAND_LATEST_URL, headers={"Accept": "application/json"})
            resp.raise_for_status()
            data = resp.json()
            return int(data["round"]), str(data["randomness"])
    except Exception:
        # Fallback to local CSPRNG if external beacon is unreachable
        return 0, secrets.token_hex(32)


def print_winner_summary(row: sqlite3.Row, already_minted: bool):
    banner = "EXISTING IMMUTABLE WINNER (ALREADY MINTED)" if already_minted else "OFFICIAL WINNER MINTED & LOCKED"
    print("=" * 72)
    print(f"  {banner}")
    print("=" * 72)
    print(f"  Maintenance Day      : {row['maintenance_day']}")
    print(f"  Winner Username      : {row['username']} (Rank #{row['winner_rank']} of {row['total_completions']})")
    print(f"  Completed At         : {row['completed_at']}")
    print(f"  Minted At            : {row['minted_at']}")
    print(f"  Strategy             : {row['strategy']}")
    print(f"  Strategy Commitment  : {row['strategy_commitment']}")
    print(f"  Commitment Preimage  : {row['commitment_preimage']}")
    if row["beacon_round"]:
        print(f"  Drand Beacon Round   : #{row['beacon_round']} (https://api.drand.sh/public/{row['beacon_round']})")
        print(f"  Drand Randomness     : {row['beacon_randomness']}")
    print(f"  Selection Hash       : {row['selection_hash']}")
    print(f"  Verification Formula : {row['verification_note']}")
    print("=" * 72)


def main():
    if len(sys.argv) < 2:
        print("Usage: python winner.py <YYYY-MM-DD>")
        sys.exit(1)

    maint_day = sys.argv[1].strip()
    challenges = load_challenges()
    config = challenges.get(maint_day)
    if not config:
        print(f"Error: No challenge configuration found for '{maint_day}' in {CHALLENGES_CONFIG_PATH}.")
        sys.exit(1)

    if not os.path.exists(DB_PATH):
        print(f"Error: Database not found at {DB_PATH}.")
        sys.exit(1)

    conn = sqlite3.connect(DB_PATH, timeout=15.0)
    conn.row_factory = sqlite3.Row
    try:
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS winners (
                maintenance_day TEXT PRIMARY KEY,
                strategy TEXT NOT NULL,
                winner_rank INTEGER NOT NULL,
                username TEXT NOT NULL,
                completed_at TEXT NOT NULL,
                total_completions INTEGER NOT NULL,
                strategy_commitment TEXT NOT NULL,
                commitment_preimage TEXT NOT NULL,
                beacon_round INTEGER,
                beacon_randomness TEXT,
                selection_hash TEXT NOT NULL,
                verification_note TEXT NOT NULL,
                minted_at TEXT NOT NULL
            )
            """
        )

        existing = conn.execute(
            "SELECT * FROM winners WHERE maintenance_day = ?",
            (maint_day,),
        ).fetchone()
        if existing is not None:
            logger.info(
                f"Winner query (already minted): day='{maint_day}', winner='{existing['username']}', rank=#{existing['winner_rank']}"
            )
            print_winner_summary(existing, already_minted=True)
            return

        rows = conn.execute(
            """
            SELECT username, completed_at
            FROM completions
            WHERE maintenance_day = ?
            ORDER BY id ASC
            """,
            (maint_day,),
        ).fetchall()

        total = len(rows)
        if total == 0:
            logger.warning(f"Winner minting aborted: no completions found for day='{maint_day}'")
            print(f"No completions recorded for maintenance day '{maint_day}' yet. Cannot mint a winner.")
            sys.exit(1)

        participants_csv = ",".join(f"{idx}:{r['username']}" for idx, r in enumerate(rows, start=1))
        strategy, preimage, commitment_hash = build_commitment_preimage(config, maint_day)
        beacon_round = None
        beacon_randomness = None

        if strategy == "first":
            winner_rank = 1
            selection_input = f"{preimage}|participants={participants_csv}"
            selection_hash = sha256_hex(selection_input)
            verification_note = "Strategy 'first': Rank #1 (first completion) wins."
        elif strategy == "nth":
            n_val = int(config.get("winner_n", 1))
            winner_rank = ((n_val - 1) % total) + 1
            selection_input = f"{preimage}|participants={participants_csv}"
            selection_hash = sha256_hex(selection_input)
            verification_note = (
                f"Strategy 'nth' (N={n_val}, total={total}): "
                f"((N - 1) % total) + 1 = (({n_val} - 1) % {total}) + 1 = Rank #{winner_rank}."
            )
        else:
            strategy = "random"
            beacon_round, beacon_randomness = fetch_drand_beacon()
            selection_input = (
                f"{preimage}|drand_round={beacon_round}|"
                f"drand_randomness={beacon_randomness}|participants={participants_csv}"
            )
            selection_hash = sha256_hex(selection_input)
            winner_rank = (int(selection_hash, 16) % total) + 1
            verification_note = (
                f"Strategy 'random': (int(sha256('{selection_input}'), 16) % {total}) + 1 = Rank #{winner_rank}"
            )

        winner_row = rows[winner_rank - 1]
        minted_at = datetime.now(HST_TZ).strftime("%Y-%m-%d %H:%M:%S HST")

        conn.execute("BEGIN IMMEDIATE")
        conn.execute(
            """
            INSERT OR IGNORE INTO winners (
                maintenance_day, strategy, winner_rank, username, completed_at,
                total_completions, strategy_commitment, commitment_preimage,
                beacon_round, beacon_randomness, selection_hash,
                verification_note, minted_at
            )
            VALUES (?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?, ?)
            """,
            (
                maint_day,
                strategy,
                winner_rank,
                winner_row["username"],
                winner_row["completed_at"],
                total,
                commitment_hash,
                preimage,
                beacon_round,
                beacon_randomness,
                selection_hash,
                verification_note,
                minted_at,
            ),
        )
        conn.commit()

        logger.info(
            f"*** OFFICIAL WINNER MINTED ***: day='{maint_day}', winner='{winner_row['username']}', "
            f"rank=#{winner_rank}/{total}, strategy='{strategy}', selection_hash='{selection_hash}'"
        )

        saved = conn.execute(
            "SELECT * FROM winners WHERE maintenance_day = ?",
            (maint_day,),
        ).fetchone()
        print_winner_summary(saved, already_minted=False)
    finally:
        conn.close()


if __name__ == "__main__":
    main()

