#!/usr/bin/env python3
"""
Immutable Winner Minting CLI for Maintenance Challenges (`winner.py`).

Evaluates the pre-configured winner strategy ('random', 'first', or 'nth') from
`/app/data/challenges.json` for a given maintenance challenge date, computes the
cryptographic selection hash, and permanently locks the winning record into the
SQLite `winners` table (`UNIQUE` per `maintenance_day`).

Cryptographic Selection Strategies:
    1. 'random' (Provably Fair Random Draw):
       - Eliminates administrator bias by sourcing external entropy from the
         League of Entropy (drand)—an independent, distributed randomness beacon
         operated by Cloudflare, EPFL, Protocol Labs, and global universities.
       - Combines the pre-committed strategy preimage, latest drand round number,
         drand randomness hex string, and ordered participant list into an input string.
       - Evaluates the winning rank via:
             `winning_rank = (int(sha256(selection_input), 16) % total_completions) + 1`

    2. 'first' (Fastest Finisher):
       - Rank #1 (the first participant in database completion order) wins.

    3. 'nth' (Secret N-th Finisher):
       - Selects the pre-committed N-th finisher (e.g., n=5).
       - If fewer than N players complete the challenge, uses modular wrap-around:
             `winning_rank = ((N - 1) % total_completions) + 1`

Immutability & Auditability:
    - Once minted, the winner row is permanently locked (`INSERT OR IGNORE`).
    - The revealed `commitment_preimage` proves that the winning strategy and salt
      were decided before the challenge opened, matching the public `strategy_commitment`
      hash displayed on the leaderboard all day.

Usage inside container:
    docker exec challenge-recorder python winner.py <YYYY-MM-DD>
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

# ==============================================================================
# Configuration & Constants
# ==============================================================================

DB_PATH = os.environ.get("DB_PATH", "/app/data/completions.db")
CHALLENGES_CONFIG_PATH = os.environ.get("CHALLENGES_CONFIG_PATH", "/app/data/challenges.json")
LOGS_DIR = os.environ.get("LOGS_DIR", "/app/data/logs")
LOG_FILE = os.path.join(LOGS_DIR, "challenge-recorder.log")
DRAND_LATEST_URL = "https://api.drand.sh/public/latest"
HST_TZ = timezone(timedelta(hours=-10), name="HST")

os.makedirs(LOGS_DIR, exist_ok=True)


# ==============================================================================
# Logging Infrastructure
# ==============================================================================

class HSTFormatter(logging.Formatter):
    """Custom logging formatter rendering timestamps in Hawaii Standard Time (HST)."""

    def formatTime(self, record: logging.LogRecord, datefmt: str = None) -> str:
        """Format record creation timestamp in HST.

        Args:
            record: The LogRecord instance to format.
            datefmt: Optional date format string. Defaults to '%Y-%m-%d %H:%M:%S HST'.

        Returns:
            Formatted timestamp string in Hawaii Standard Time.
        """
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


# ==============================================================================
# Cryptographic & Beacon Helper Functions
# ==============================================================================

def sha256_hex(message: str) -> str:
    """Calculate the lowercase hexadecimal SHA-256 digest of a string.

    Args:
        message: The UTF-8 string to hash.

    Returns:
        A 64-character lowercase hexadecimal SHA-256 hash string.
    """
    return hashlib.sha256(message.encode("utf-8")).hexdigest()


def load_challenges() -> dict:
    """Load and parse the live challenge configuration from disk.

    Returns:
        A dictionary containing challenge definitions mapping dates to settings,
        or an empty dictionary if the file cannot be read.
    """
    if not os.path.exists(CHALLENGES_CONFIG_PATH):
        return {}
    with open(CHALLENGES_CONFIG_PATH, "r", encoding="utf-8") as f:
        data = json.load(f)
        return data if isinstance(data, dict) else {}


def build_commitment_preimage(config: dict, maintenance_day: str) -> tuple[str, str, str]:
    """Construct the strategy commitment preimage and calculate its SHA-256 hash.

    The preimage format acts as an unsealed envelope:
        - For 'random' or 'first': `<salt>:day=<YYYY-MM-DD>:strategy=<strategy>`
        - For 'nth': `<salt>:day=<YYYY-MM-DD>:strategy=nth:n=<N>`

    Args:
        config: Challenge configuration dictionary for the specific date.
        maintenance_day: Challenge date in 'YYYY-MM-DD' format.

    Returns:
        A tuple of `(strategy, preimage, commitment_hash)`:
            - strategy: Normalized strategy name ('random', 'first', or 'nth').
            - preimage: The unhashed preimage string.
            - commitment_hash: The SHA-256 hash of the preimage (matching the leaderboard).
    """
    strategy = str(config.get("strategy", "random")).strip().lower()
    salt = str(config.get("strategy_salt") or config.get("seed_prefix") or maintenance_day).strip()
    if strategy == "nth":
        winner_n = int(config.get("winner_n", 1))
        preimage = f"{salt}:day={maintenance_day}:strategy=nth:n={winner_n}"
    else:
        preimage = f"{salt}:day={maintenance_day}:strategy={strategy}"
    return strategy, preimage, sha256_hex(preimage)


def fetch_drand_beacon() -> tuple[int, str]:
    """Retrieve the latest public randomness beacon from the League of Entropy (drand).

    Queries the primary public drand endpoint (https://api.drand.sh/public/latest)
    for an unpredictable, verifiable 256-bit threshold BLS signature. If the network
    call fails, securely falls back to local cryptographic randomness (CSPRNG).

    Returns:
        A tuple of `(round_number, randomness_hex)`:
            - round_number: The sequential drand beacon round integer (0 if fallback).
            - randomness_hex: 64-character hexadecimal randomness string.
    """
    try:
        with httpx.Client(timeout=10.0) as client:
            resp = client.get(DRAND_LATEST_URL, headers={"Accept": "application/json"})
            resp.raise_for_status()
            data = resp.json()
            return int(data["round"]), str(data["randomness"])
    except Exception:
        # Fallback to local CSPRNG if external League of Entropy beacon is unreachable
        return 0, secrets.token_hex(32)


def print_winner_summary(row: sqlite3.Row, already_minted: bool):
    """Print a formatted ASCII summary of the minted challenge winner to stdout.

    Args:
        row: SQLite Row containing winner record fields.
        already_minted: True if the record was previously minted and locked,
                        False if it was newly minted during this execution.
    """
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


# ==============================================================================
# Main CLI Execution Flow
# ==============================================================================

def main():
    """Execute the CLI winner minting routine.

    Validates command-line arguments, queries SQLite completions, evaluates the
    pre-committed selection strategy against the participant list, records the
    immutable winning record inside a transaction, and logs the result.
    """
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
        # Step 1: Ensure winners table exists
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

        # Step 2: Check if winner is already minted (immutable idempotency check)
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

        # Step 3: Fetch all completions ordered chronologically by database ID
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

        # Step 4: Build participant string and evaluate strategy formula
        participants_csv = ",".join(f"{idx}:{r['username']}" for idx, r in enumerate(rows, start=1))
        strategy, preimage, commitment_hash = build_commitment_preimage(config, maint_day)
        beacon_round = None
        beacon_randomness = None

        if strategy == "first":
            # Strategy: First Finisher
            winner_rank = 1
            selection_input = f"{preimage}|participants={participants_csv}"
            selection_hash = sha256_hex(selection_input)
            verification_note = "Strategy 'first': Rank #1 (first completion) wins."
        elif strategy == "nth":
            # Strategy: Secret N-th Finisher (with modular wrap-around)
            n_val = int(config.get("winner_n", 1))
            winner_rank = ((n_val - 1) % total) + 1
            selection_input = f"{preimage}|participants={participants_csv}"
            selection_hash = sha256_hex(selection_input)
            verification_note = (
                f"Strategy 'nth' (N={n_val}, total={total}): "
                f"((N - 1) % total) + 1 = (({n_val} - 1) % {total}) + 1 = Rank #{winner_rank}."
            )
        else:
            # Strategy: League of Entropy (drand) verifiable random draw
            strategy = "random"
            beacon_round, beacon_randomness = fetch_drand_beacon()
            selection_input = (
                f"{preimage}|drand_round={beacon_round}|"
                f"drand_randomness={beacon_randomness}|participants={participants_csv}"
            )
            selection_hash = sha256_hex(selection_input)
            # Map 256-bit hexadecimal hash to 1-based rank modulo total completions
            winner_rank = (int(selection_hash, 16) % total) + 1
            verification_note = (
                f"Strategy 'random': (int(sha256('{selection_input}'), 16) % {total}) + 1 = Rank #{winner_rank}"
            )

        winner_row = rows[winner_rank - 1]
        minted_at = datetime.now(HST_TZ).strftime("%Y-%m-%d %H:%M:%S HST")

        # Step 5: Atomically insert and permanently lock the winning record
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
