# Post-Maintenance Challenge Recorder (`challenge-recorder`)

Lightweight Python/FastAPI service that verifies DisasterAWARE Pro JWT tokens and cryptographic completion proofs (or single-stage riddle answers) for weekly post-maintenance challenges, persisting completion ranks, HST timestamps, and immutable cryptographically verifiable winners in SQLite (`/app/data/completions.db`).

---

## 1. Directory & Data Files

- `main.py` — FastAPI application (`/api/complete`, `/api/leaderboard`, `/api/riddle`, `/api/health`).
- `winner.py` — CLI utility to mint and lock the official challenge winner for a maintenance date (`random`, `first`, or `nth` strategy).
- `reset.py` — CLI utility to reset completions (and any test-minted winner) in SQLite for a given maintenance date or single user.
- `challenges.example.json` — Template showing how weekly challenge keys/answers and winner strategies are configured.
- `data/challenges.json` *(git-ignored)* — Live weekly challenge configuration loaded dynamically on every request (no container rebuild or restart required when adding new weekly challenges).
- `data/completions.db` *(git-ignored)* — SQLite database storing verified completions and immutable winners per `maintenance_day`.

---

## 2. Minting the Official Winner (`winner.py`)

When the challenge submission window closes (e.g., noon HST on Thursday), run `winner.py` from the `fester-server` shell to select and permanently lock the winner for that maintenance date:

```bash
docker exec challenge-recorder python winner.py 2026-11-03
```

### How Winner Strategies & Anti-Favorites Proof Work
In `data/challenges.json`, each maintenance day defines its winner strategy and a secret `strategy_salt`:
- `"strategy": "random"` — Fetches the latest public **League of Entropy (`drand`) randomness beacon** (`https://api.drand.sh/public/latest`) and hashes it with the pre-committed strategy preimage and ordered participant list (`(int(sha256(...), 16) % total) + 1`).
- `"strategy": "first"` — Rank `#1` (the fastest completion) wins.
- `"strategy": "nth"` (with `"winner_n": N`) — The $N$-th finisher wins (`((N - 1) % total) + 1`).

**Pre-Commitment Verification**:
- While the challenge is open, `/api/leaderboard` publishes a `strategy_commitment` SHA-256 hash (`sha256(strategy_salt + ":day=" + day + ":strategy=" + ...)`). Because `strategy_salt` is only stored on the server in `data/challenges.json`, players cannot tell whether the rule is `random`, `first`, or `nth=N` while playing.
- Once `winner.py` runs, the winner is locked into the SQLite `winners` table (`UNIQUE(maintenance_day)` — running `winner.py` again cannot overwrite it!) and `/api/leaderboard` reveals the `commitment_preimage`, `beacon_round`, `selection_hash`, and exact verification formula so anyone can verify the rule was locked in from the start.

---

## 3. Resetting Test Runs (`reset.py`)

After running through a challenge (and optionally testing `winner.py`) before maintenance ends, you can clear the test records from `completions.db` without affecting other maintenance dates:

### Clear all completions (and any test-minted winner) for a specific challenge date
```bash
docker exec challenge-recorder python reset.py 2026-11-03
```

### Delete only a single user's test completion for a date (and re-sequence remaining ranks)
```bash
docker exec challenge-recorder python reset.py 2026-11-03 skunitzer
```

---

## 4. Viewing the Leaderboard

Completions are ordered from first to last (`ORDER BY id ASC`) with timestamps in `HST`:

- **JSON (default)**:
  - `https://kunitzer.me/api/leaderboard?day=2026-11-03`
  - `https://kunitzer.me/api/leaderboard?day=2026-11-03&f=json`
  - `https://kunitzer.me/api/leaderboard/2026-11-03`
- **HTML Table (`f=html`)**:
  - `https://kunitzer.me/api/leaderboard?day=2026-11-03&f=html`
  - `https://kunitzer.me/api/leaderboard/2026-11-03?f=html`
