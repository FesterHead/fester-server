#!/usr/bin/env python3
"""
Utility script to clear test submissions (and any test-minted winner) from
completions.db for a given maintenance day (or a specific user on that maintenance day)
and re-sequence ranks cleanly.

Usage inside container:
  docker exec challenge-recorder python reset.py 2026-11-03
  docker exec challenge-recorder python reset.py 2026-11-03 skunitzer
"""

import logging
import os
import sqlite3
import sys
from datetime import datetime, timedelta, timezone
from logging.handlers import RotatingFileHandler

DB_PATH = os.environ.get("DB_PATH", "/app/data/completions.db")
LOGS_DIR = os.environ.get("LOGS_DIR", "/app/data/logs")
LOG_FILE = os.path.join(LOGS_DIR, "challenge-recorder.log")
HST_TZ = timezone(timedelta(hours=-10), name="HST")

os.makedirs(LOGS_DIR, exist_ok=True)


class HSTFormatter(logging.Formatter):
    def formatTime(self, record, datefmt=None):
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


def main():
    if len(sys.argv) < 2:
        print("Usage: python reset.py <YYYY-MM-DD> [username]")
        sys.exit(1)

    maint_day = sys.argv[1].strip()
    username = sys.argv[2].strip().lower() if len(sys.argv) >= 3 else None

    if not os.path.exists(DB_PATH):
        print(f"Database file not found at {DB_PATH} (nothing to reset).")
        return

    conn = sqlite3.connect(DB_PATH, timeout=15.0)
    conn.row_factory = sqlite3.Row
    try:
        conn.execute("BEGIN IMMEDIATE")
        if username:
            cur = conn.execute(
                "DELETE FROM completions WHERE maintenance_day = ? AND username = ?",
                (maint_day, username),
            )
            deleted = cur.rowcount
            rows = conn.execute(
                "SELECT id FROM completions WHERE maintenance_day = ? ORDER BY id ASC",
                (maint_day,),
            ).fetchall()
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
            cur = conn.execute(
                "DELETE FROM completions WHERE maintenance_day = ?",
                (maint_day,),
            )
            deleted = cur.rowcount
            w_deleted = 0
            try:
                w_cur = conn.execute(
                    "DELETE FROM winners WHERE maintenance_day = ?",
                    (maint_day,),
                )
                w_deleted = w_cur.rowcount
            except sqlite3.OperationalError:
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

        # Keep sqlite_sequence in sync so AUTOINCREMENT starts at 1 when the table is empty
        remaining_total = conn.execute("SELECT COUNT(*) AS cnt FROM completions").fetchone()["cnt"]
        if remaining_total == 0:
            conn.execute("DELETE FROM sqlite_sequence WHERE name = 'completions'")
        else:
            max_id = conn.execute("SELECT MAX(id) AS max_id FROM completions").fetchone()["max_id"]
            conn.execute("UPDATE sqlite_sequence SET seq = ? WHERE name = 'completions'", (max_id,))
        conn.commit()
        conn.execute("PRAGMA wal_checkpoint(TRUNCATE)")
    finally:
        conn.close()


if __name__ == "__main__":
    main()
