#!/usr/bin/env python3
"""
Challenge Database Reset & Rank Re-sequencing CLI (`reset.py`).

Provides administrative maintenance routines for clearing test submissions and
minted winner records from `/app/data/completions.db`. Supports granular single-user
purging with automatic rank re-sequencing as well as full-day challenge wipes.

Operational Modes:
    1. Single-User Reset:
       - Command: `docker exec challenge-recorder python reset.py <YYYY-MM-DD> <username>`
       - Deletes all completions for the specified user on the targeted maintenance day.
       - Re-sequences the 1-based `rank` field for all remaining completions for that day
         in strict chronological order (`ORDER BY id ASC`), eliminating ranking gaps.
       - Preserves other participants' data and any existing winner records.

    2. Full-Day Reset:
       - Command: `docker exec challenge-recorder python reset.py <YYYY-MM-DD>`
       - Purges all completions for the specified maintenance day.
       - Purges any minted winner record for that day from the `winners` table.

Post-Reset Maintenance & Integrity:
    - Auto-Increment Sequence Synchronization:
      Synchronizes SQLite's internal `sqlite_sequence` table with the actual maximum
      row ID in `completions`. If the table is empty, deletes the sequence record so
      future submissions restart cleanly at ID 1.
    - Write-Ahead Logging (WAL) Checkpoint:
      Executes `PRAGMA wal_checkpoint(TRUNCATE)` to flush all database changes from the
      WAL journal into the primary database file and truncate the journal.
    - Audit Logging:
      Logs all reset operations to `/app/data/logs/challenge-recorder.log` with Hawaii
      Standard Time (HST) timestamps.

Usage inside container:
    docker exec challenge-recorder python reset.py <YYYY-MM-DD> [username]
"""

import logging
import os
import sqlite3
import sys
from datetime import datetime, timedelta, timezone
from logging.handlers import RotatingFileHandler

# ==============================================================================
# Configuration & Constants
# ==============================================================================

DB_PATH = os.environ.get("DB_PATH", "/app/data/completions.db")
LOGS_DIR = os.environ.get("LOGS_DIR", "/app/data/logs")
LOG_FILE = os.path.join(LOGS_DIR, "challenge-recorder.log")
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


logger = logging.getLogger("challenge-recorder-reset")
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
# Main CLI Execution Flow
# ==============================================================================

def main():
    """Execute the database reset and re-sequencing CLI routine.

    Parses command-line arguments, opens a transactional write connection to the
    SQLite database, executes either single-user deletion with rank re-sequencing
    or full-day purging, synchronizes auto-increment counters, checkpoints WAL,
    and writes audit log entries.
    """
    # Step 1: Validate command-line arguments
    if len(sys.argv) < 2:
        print("Usage: python reset.py <YYYY-MM-DD> [username]")
        sys.exit(1)

    maint_day = sys.argv[1].strip()
    username = sys.argv[2].strip().lower() if len(sys.argv) >= 3 else None

    # Step 2: Check database existence
    if not os.path.exists(DB_PATH):
        print(f"Database file not found at {DB_PATH} (nothing to reset).")
        return

    conn = sqlite3.connect(DB_PATH, timeout=15.0)
    conn.row_factory = sqlite3.Row
    try:
        # Step 3: Begin immediate transaction to prevent concurrent race conditions
        conn.execute("BEGIN IMMEDIATE")

        if username:
            # ------------------------------------------------------------------
            # Mode 1: Single-User Purge & Rank Re-sequencing
            # ------------------------------------------------------------------
            # Delete submissions matching the target maintenance day and username
            cur = conn.execute(
                "DELETE FROM completions WHERE maintenance_day = ? AND username = ?",
                (maint_day, username),
            )
            deleted = cur.rowcount

            # Fetch remaining submissions for this maintenance day ordered chronologically
            rows = conn.execute(
                "SELECT id FROM completions WHERE maintenance_day = ? ORDER BY id ASC",
                (maint_day,),
            ).fetchall()

            # Re-sequence ranks consecutively (1, 2, 3...) to eliminate gaps
            for new_rank, r in enumerate(rows, start=1):
                conn.execute(
                    "UPDATE completions SET rank = ? WHERE id = ?",
                    (new_rank, r["id"]),
                )
            conn.commit()

            logger.info(
                f"RESET: Removed {deleted} record(s) for user='{username}' on day='{maint_day}'. "
                f"Remaining completions: {len(rows)}."
            )
            print(
                f"Removed {deleted} record(s) for user '{username}' on {maint_day}. "
                f"Remaining completions for {maint_day}: {len(rows)}."
            )
        else:
            # ------------------------------------------------------------------
            # Mode 2: Full-Day Purge (Completions & Winners)
            # ------------------------------------------------------------------
            # Delete all completion records for the specified maintenance day
            cur = conn.execute(
                "DELETE FROM completions WHERE maintenance_day = ?",
                (maint_day,),
            )
            deleted = cur.rowcount

            # Delete any minted winner record for this day from the winners table
            w_deleted = 0
            try:
                w_cur = conn.execute(
                    "DELETE FROM winners WHERE maintenance_day = ?",
                    (maint_day,),
                )
                w_deleted = w_cur.rowcount
            except sqlite3.OperationalError:
                # winners table does not exist yet; safe to ignore
                pass
            conn.commit()

            logger.info(
                f"RESET: Cleared {deleted} completion record(s) and {w_deleted} winner record(s) "
                f"for day='{maint_day}'."
            )
            print(
                f"Cleared all {deleted} completion record(s) and {w_deleted} winner record(s) "
                f"for maintenance day {maint_day}."
            )

        # ----------------------------------------------------------------------
        # Step 4: Synchronize SQLite Auto-Increment Counter (sqlite_sequence)
        # ----------------------------------------------------------------------
        # Ensures that auto-increment ID resets to 1 if table is completely empty,
        # or stays anchored to the true MAX(id) if records remain.
        remaining_total = conn.execute("SELECT COUNT(*) AS cnt FROM completions").fetchone()["cnt"]
        if remaining_total == 0:
            conn.execute("DELETE FROM sqlite_sequence WHERE name = 'completions'")
        else:
            max_id = conn.execute("SELECT MAX(id) AS max_id FROM completions").fetchone()["max_id"]
            conn.execute("UPDATE sqlite_sequence SET seq = ? WHERE name = 'completions'", (max_id,))
        conn.commit()

        # ----------------------------------------------------------------------
        # Step 5: Flush and Truncate Write-Ahead Logging (WAL)
        # ----------------------------------------------------------------------
        conn.execute("PRAGMA wal_checkpoint(TRUNCATE)")
    finally:
        conn.close()


if __name__ == "__main__":
    main()
