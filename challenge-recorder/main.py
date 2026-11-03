import hashlib
import html
import json
import os
import re
import sqlite3
from contextlib import asynccontextmanager, contextmanager
from datetime import datetime, timedelta, timezone
from typing import Optional

import httpx
import jwt
from fastapi import FastAPI, Header, HTTPException, Query, status
from fastapi.middleware.cors import CORSMiddleware
from fastapi.responses import HTMLResponse, JSONResponse
from jwt import PyJWKClient
from pydantic import BaseModel, Field

DB_PATH = os.environ.get("DB_PATH", "/app/data/completions.db")
CHALLENGES_CONFIG_PATH = os.environ.get("CHALLENGES_CONFIG_PATH", "/app/data/challenges.json")
DISASTERAWARE_LOGIN_URL = "https://disasteraware.pdc.org/jwt/v1/auth/login"
DISASTERAWARE_VALIDATE_URL = "https://disasteraware.pdc.org/jwt/v1/auth/validate"
DISASTERAWARE_JWKS_ISSUER = "https://disasteraware.pdc.org/jwt/jwks.json"
HST_TZ = timezone(timedelta(hours=-10), name="HST")

jwks_client = PyJWKClient(DISASTERAWARE_JWKS_ISSUER, cache_keys=True)


def load_challenges() -> dict:
    if not os.path.exists(CHALLENGES_CONFIG_PATH):
        return {}
    try:
        with open(CHALLENGES_CONFIG_PATH, "r", encoding="utf-8") as f:
            data = json.load(f)
            return data if isinstance(data, dict) else {}
    except (OSError, ValueError):
        return {}


def get_default_maint_day(challenges: Optional[dict] = None) -> str:
    cfg = challenges if challenges is not None else load_challenges()
    if cfg:
        return sorted(cfg.keys())[-1]
    return datetime.now(HST_TZ).strftime("%Y-%m-%d")


def sha256_hex(message: str) -> str:
    return hashlib.sha256(message.encode("utf-8")).hexdigest()


def normalize_key(raw: str) -> str:
    return re.sub(r"[^a-z0-9]", "", raw.strip().lower())


def compute_strategy_commitment(maintenance_day: str) -> Optional[str]:
    challenges = load_challenges()
    config = challenges.get(maintenance_day)
    if not config:
        return None
    strategy = str(config.get("strategy", "random")).strip().lower()
    salt = str(config.get("strategy_salt") or config.get("seed_prefix") or maintenance_day).strip()
    if strategy == "nth":
        winner_n = int(config.get("winner_n", 1))
        preimage = f"{salt}:day={maintenance_day}:strategy=nth:n={winner_n}"
    else:
        preimage = f"{salt}:day={maintenance_day}:strategy={strategy}"
    return sha256_hex(preimage)


def get_cutoff_info(maintenance_day: str) -> tuple[datetime, str]:
    challenges = load_challenges()
    config = challenges.get(maintenance_day)
    if not config:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"No challenge configuration found for maintenance day '{maintenance_day}'.",
        )
    raw = str(config.get("cutoff", "")).strip()
    if not raw:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Challenge configuration for '{maintenance_day}' is missing required 'cutoff' timestamp.",
        )
    cleaned = re.sub(r"\s+HST$", "", raw, flags=re.IGNORECASE).strip()
    for fmt in ("%Y-%m-%d %H:%M:%S", "%Y-%m-%d %H:%M", "%Y-%m-%d"):
        try:
            dt = datetime.strptime(cleaned, fmt).replace(tzinfo=HST_TZ)
            return dt, dt.strftime("%Y-%m-%d %H:%M:%S HST")
        except ValueError:
            pass
    try:
        dt = datetime.fromisoformat(cleaned)
        if dt.tzinfo is None:
            dt = dt.replace(tzinfo=HST_TZ)
        else:
            dt = dt.astimezone(HST_TZ)
        return dt, dt.strftime("%Y-%m-%d %H:%M:%S HST")
    except ValueError as exc:
        raise HTTPException(
            status_code=status.HTTP_500_INTERNAL_SERVER_ERROR,
            detail=f"Invalid 'cutoff' timestamp format '{raw}' for maintenance day '{maintenance_day}'.",
        ) from exc


def compute_expected_proof(username: str, maintenance_day: str) -> str:
    challenges = load_challenges()
    config = challenges.get(maintenance_day)
    if not config:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"No challenge configuration found for maintenance day '{maintenance_day}'.",
        )

    keys = config.get("keys")
    if not keys and config.get("answer"):
        keys = [config["answer"]]

    if not isinstance(keys, list) or not keys:
        raise HTTPException(
            status_code=status.HTTP_404_NOT_FOUND,
            detail=f"No challenge keys configured for maintenance day '{maintenance_day}'.",
        )

    default_prefix = f"post_maint_{maintenance_day.replace('-', '_')}:"
    seed_prefix = config.get("seed_prefix", default_prefix)
    clean_user = username.strip().lower()
    chain = sha256_hex(f"{seed_prefix}{clean_user}")
    for key in keys:
        chain = sha256_hex(f"{chain}:{normalize_key(str(key))}")
    return chain


def compute_proof_from_single_answer(username: str, answer: str, maintenance_day: str) -> str:
    challenges = load_challenges()
    config = challenges.get(maintenance_day) or {}
    default_prefix = f"post_maint_{maintenance_day.replace('-', '_')}:"
    seed_prefix = config.get("seed_prefix", default_prefix)
    clean_user = username.strip().lower()
    seed = sha256_hex(f"{seed_prefix}{clean_user}")
    return sha256_hex(f"{seed}:{normalize_key(answer)}")


@contextmanager
def get_db():
    os.makedirs(os.path.dirname(DB_PATH), exist_ok=True)
    conn = sqlite3.connect(DB_PATH, timeout=15.0)
    conn.row_factory = sqlite3.Row
    try:
        conn.execute("PRAGMA journal_mode=WAL;")
        yield conn
        conn.commit()
    finally:
        conn.close()


def init_db():
    with get_db() as conn:
        conn.execute(
            """
            CREATE TABLE IF NOT EXISTS completions (
                id INTEGER PRIMARY KEY AUTOINCREMENT,
                maintenance_day TEXT NOT NULL,
                username TEXT NOT NULL,
                proof TEXT NOT NULL,
                completed_at TEXT NOT NULL,
                rank INTEGER NOT NULL,
                UNIQUE(maintenance_day, username)
            )
            """
        )
        conn.execute(
            """
            CREATE INDEX IF NOT EXISTS idx_completions_day_id
            ON completions (maintenance_day, id ASC)
            """
        )
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


@asynccontextmanager
async def lifespan(app: FastAPI):
    init_db()
    yield


app = FastAPI(
    title="Post-Maintenance Challenge Recorder",
    version="1.1.0",
    lifespan=lifespan,
)

app.add_middleware(
    CORSMiddleware,
    allow_origins=["*"],
    allow_credentials=True,
    allow_methods=["*"],
    allow_headers=["*"],
)


class LoginRequest(BaseModel):
    username: str
    password: str


class CompleteRequest(BaseModel):
    user: str = Field(..., min_length=1)
    proof: Optional[str] = Field(default=None, description="Rolling SHA-256 proof chain")
    answer: Optional[str] = Field(default=None, description="Direct answer for single-stage riddle challenges")
    maintenance_day: Optional[str] = Field(default=None)


async def decode_and_verify_jwt(authorization: Optional[str]) -> dict:
    if not authorization:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Missing Authorization header.",
        )
    if authorization.lower().startswith("bearer "):
        token = authorization.split(" ", 1)[1].strip()
    else:
        token = authorization.strip()

    if not token:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="Empty JWT token.",
        )

    try:
        unverified_header = jwt.get_unverified_header(token)
        unverified_claims = jwt.decode(
            token,
            options={"verify_signature": False, "verify_aud": False},
        )
    except jwt.PyJWTError as exc:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail=f"Malformed JWT token: {exc}",
        ) from exc

    iss = unverified_claims.get("iss")
    if iss != DISASTERAWARE_JWKS_ISSUER:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail=f"Untrusted JWT issuer: '{iss}'.",
        )

    alg = unverified_header.get("alg", "RS256")
    try:
        signing_key = jwks_client.get_signing_key_from_jwt(token)
        verified_claims = jwt.decode(
            token,
            signing_key.key,
            algorithms=[alg],
            issuer=DISASTERAWARE_JWKS_ISSUER,
            options={"verify_aud": False, "verify_exp": False},
        )
    except Exception as exc:
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail=f"JWT signature verification failed: {exc}",
        ) from exc

    sub = verified_claims.get("sub")
    if not sub or not str(sub).strip():
        raise HTTPException(
            status_code=status.HTTP_401_UNAUTHORIZED,
            detail="JWT token missing 'sub' identity claim.",
        )
    return verified_claims


@app.get("/api/health")
async def health():
    return {"status": "ok"}


@app.post("/api/auth/login")
async def proxy_disasteraware_login(payload: LoginRequest):
    try:
        async with httpx.AsyncClient(timeout=15.0) as client:
            resp = await client.post(
                DISASTERAWARE_LOGIN_URL,
                json={"username": payload.username, "password": payload.password},
                headers={
                    "Accept": "application/json",
                    "Content-Type": "application/json",
                },
            )
    except httpx.RequestError as exc:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail=f"Unable to reach DisasterAWARE authentication service: {exc}",
        ) from exc

    if resp.status_code in (401, 403):
        raise HTTPException(
            status_code=resp.status_code,
            detail="Invalid DisasterAWARE Pro username or password.",
        )

    if resp.status_code >= 400:
        raise HTTPException(
            status_code=resp.status_code,
            detail=f"DisasterAWARE authentication returned HTTP {resp.status_code}.",
        )

    try:
        data = resp.json()
    except ValueError as exc:
        raise HTTPException(
            status_code=status.HTTP_502_BAD_GATEWAY,
            detail="Invalid JSON response from DisasterAWARE authentication service.",
        ) from exc

    return JSONResponse(status_code=resp.status_code, content=data)


@app.post("/api/complete")
async def record_completion(
    payload: CompleteRequest,
    authorization: Optional[str] = Header(default=None),
):
    claims = await decode_and_verify_jwt(authorization)
    jwt_user = str(claims.get("sub", "")).strip().lower()
    req_user = payload.user.strip().lower()

    if jwt_user != req_user:
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail=f"Identity mismatch: JWT sub '{jwt_user}' does not match submitted user '{req_user}'.",
        )

    maint_day = (payload.maintenance_day or get_default_maint_day()).strip()
    expected_proof = compute_expected_proof(jwt_user, maint_day)

    if payload.proof:
        submitted_proof = payload.proof.strip().lower()
    elif payload.answer:
        submitted_proof = compute_proof_from_single_answer(jwt_user, payload.answer, maint_day)
    else:
        raise HTTPException(
            status_code=status.HTTP_400_BAD_REQUEST,
            detail="Submission must include either 'proof' or 'answer'.",
        )

    if submitted_proof != expected_proof.lower():
        raise HTTPException(
            status_code=status.HTTP_403_FORBIDDEN,
            detail="Verification failed. Incorrect answer or incomplete stage progression.",
        )

    now_dt = datetime.now(HST_TZ)
    now_hst = now_dt.strftime("%Y-%m-%d %H:%M:%S HST")
    cutoff_dt, cutoff_display = get_cutoff_info(maint_day)

    with get_db() as conn:
        conn.execute("BEGIN IMMEDIATE")
        row = conn.execute(
            """
            SELECT rank, completed_at, maintenance_day
            FROM completions
            WHERE maintenance_day = ? AND username = ?
            """,
            (maint_day, jwt_user),
        ).fetchone()

        if row is not None:
            return {
                "status": "already_recorded",
                "user": jwt_user,
                "maintenance_day": row["maintenance_day"],
                "rank": row["rank"],
                "recorded_at": row["completed_at"],
            }

        existing_winner = conn.execute(
            "SELECT username FROM winners WHERE maintenance_day = ?",
            (maint_day,),
        ).fetchone()
        if existing_winner is not None:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail=f"Submissions closed! The official winner has already been minted for maintenance challenge {maint_day}.",
            )

        if now_dt > cutoff_dt:
            raise HTTPException(
                status_code=status.HTTP_403_FORBIDDEN,
                detail=(
                    f"Submissions closed! The cutoff for maintenance challenge {maint_day} "
                    f"was {cutoff_display} (submitted at {now_hst})."
                ),
            )

        next_rank_row = conn.execute(
            "SELECT COUNT(*) + 1 AS next_rank FROM completions WHERE maintenance_day = ?",
            (maint_day,),
        ).fetchone()
        next_rank = int(next_rank_row["next_rank"]) if next_rank_row else 1

        conn.execute(
            """
            INSERT OR IGNORE INTO completions
                (maintenance_day, username, proof, completed_at, rank)
            VALUES (?, ?, ?, ?, ?)
            """,
            (maint_day, jwt_user, expected_proof, now_hst, next_rank),
        )

        saved = conn.execute(
            """
            SELECT rank, completed_at, maintenance_day
            FROM completions
            WHERE maintenance_day = ? AND username = ?
            """,
            (maint_day, jwt_user),
        ).fetchone()

    return {
        "status": "success",
        "user": jwt_user,
        "maintenance_day": saved["maintenance_day"],
        "rank": saved["rank"],
        "recorded_at": saved["completed_at"],
    }


def get_winner_dict(conn: sqlite3.Connection, maintenance_day: str) -> Optional[dict]:
    row = conn.execute(
        "SELECT * FROM winners WHERE maintenance_day = ?",
        (maintenance_day,),
    ).fetchone()
    if row is None:
        return None
    return {
        "username": row["username"],
        "winner_rank": row["winner_rank"],
        "completed_at": row["completed_at"],
        "total_completions": row["total_completions"],
        "strategy": row["strategy"],
        "strategy_commitment": row["strategy_commitment"],
        "commitment_preimage": row["commitment_preimage"],
        "beacon_round": row["beacon_round"],
        "beacon_randomness": row["beacon_randomness"],
        "selection_hash": row["selection_hash"],
        "verification_note": row["verification_note"],
        "minted_at": row["minted_at"],
    }


def render_leaderboard_response(maintenance_day: Optional[str], fmt: str):
    day = (maintenance_day or get_default_maint_day()).strip()
    output_format = (fmt or "json").strip().lower()
    commitment = compute_strategy_commitment(day)
    _, cutoff_display = get_cutoff_info(day)

    with get_db() as conn:
        if day.lower() == "all":
            rows = conn.execute(
                """
                SELECT rank, username, completed_at, maintenance_day
                FROM completions
                ORDER BY id ASC
                """
            ).fetchall()
            winner_info = None
        else:
            rows = conn.execute(
                """
                SELECT rank, username, completed_at, maintenance_day
                FROM completions
                WHERE maintenance_day = ?
                ORDER BY id ASC
                """,
                (day,),
            ).fetchall()
            winner_info = get_winner_dict(conn, day)

    completions = [
        {
            "rank": idx,
            "username": r["username"],
            "completed_at": r["completed_at"],
        }
        for idx, r in enumerate(rows, start=1)
    ]

    if output_format == "html":
        safe_day = html.escape(day)
        winner_rank = winner_info["winner_rank"] if winner_info else None

        if completions:
            table_rows_list = []
            for item in completions:
                is_win = winner_rank == item["rank"]
                row_class = ' class="winner-row"' if is_win else ""
                badge = " 🏆" if is_win else ""
                table_rows_list.append(
                    f"<tr{row_class}>"
                    f"<td>{item['rank']}</td>"
                    f"<td>{html.escape(str(item['username']))}{badge}</td>"
                    f"<td>{html.escape(str(item['completed_at']))}</td>"
                    f"</tr>"
                )
            table_rows = "\n".join(table_rows_list)
        else:
            table_rows = '<tr><td colspan="3" style="text-align:center;color:#9ca3af;">No completions recorded yet.</td></tr>'

        if winner_info:
            beacon_line = ""
            if winner_info.get("beacon_round"):
                b_round = int(winner_info["beacon_round"])
                b_rand = html.escape(str(winner_info["beacon_randomness"]))
                beacon_line = (
                    f"<div><strong>Drand Beacon Round:</strong> "
                    f'<a href="https://api.drand.sh/public/{b_round}" target="_blank" rel="noopener noreferrer" style="color:#38bdf8;">#{b_round}</a> '
                    f"(<code>{b_rand}</code>)</div>"
                )
            winner_html = f"""
    <div class="winner-box">
      <div class="winner-title">🏆 Official Challenge Winner: {html.escape(str(winner_info['username']))} (Rank #{winner_info['winner_rank']})</div>
      <div class="winner-details">
        <div><strong>Minted At:</strong> {html.escape(str(winner_info['minted_at']))} &bull; <strong>Strategy:</strong> <code>{html.escape(str(winner_info['strategy']))}</code></div>
        <div><strong>Strategy Commitment (SHA-256):</strong> <code>{html.escape(str(winner_info['strategy_commitment']))}</code></div>
        <div><strong>Commitment Preimage:</strong> <code>{html.escape(str(winner_info['commitment_preimage']))}</code></div>
        {beacon_line}
        <div><strong>Selection Hash:</strong> <code>{html.escape(str(winner_info['selection_hash']))}</code></div>
        <div><strong>Verification:</strong> <code>{html.escape(str(winner_info['verification_note']))}</code></div>
        <div style="margin-top:0.55rem;"><a href="/maintenance/fairness.html?day={safe_day}" style="color:#38bdf8;font-weight:bold;text-decoration:none;">🔍 Verify Winner &amp; Read How Cryptographic Selection Works &rarr;</a></div>
      </div>
    </div>"""
        elif commitment:
            winner_html = f"""
    <div class="commit-box">
      <strong>🔒 Pre-Committed Winner Strategy Hash (SHA-256):</strong><br/>
      <code>{html.escape(commitment)}</code>
      <div style="margin-top:0.45rem;"><a href="/maintenance/fairness.html?day={safe_day}" style="color:#38bdf8;font-weight:bold;text-decoration:none;">🔍 How Winner Selection &amp; Verification Works &rarr;</a></div>
    </div>"""
        else:
            winner_html = ""

        html_doc = f"""<!DOCTYPE html>
<html lang="en">
<head>
  <meta charset="UTF-8" />
  <meta name="viewport" content="width=device-width, initial-scale=1.0" />
  <meta name="color-scheme" content="dark" />
  <meta name="darkreader-lock" />
  <title>Maintenance Challenge Leaderboard ({safe_day})</title>
  <style>
    body {{
      background: #0b0f19;
      color: #e5e7eb;
      font-family: ui-monospace, SFMono-Regular, Menlo, Monaco, Consolas, monospace;
      padding: 2rem 1rem;
      margin: 0;
      display: flex;
      justify-content: center;
    }}
    .wrap {{
      max-width: 760px;
      width: 100%;
      background: #111827;
      border: 1px solid #1f2937;
      border-radius: 10px;
      padding: 1.75rem;
    }}
    h1 {{
      margin: 0 0 0.5rem 0;
      font-size: 1.35rem;
      color: #38bdf8;
    }}
    .meta {{
      font-size: 0.85rem;
      color: #9ca3af;
      margin-bottom: 1.25rem;
    }}
    .commit-box {{
      background: #0f172a;
      border: 1px solid #334155;
      border-left: 4px solid #38bdf8;
      border-radius: 6px;
      padding: 0.75rem 1rem;
      margin-bottom: 1.25rem;
      font-size: 0.78rem;
      line-height: 1.5;
      color: #cbd5e1;
      word-break: break-all;
    }}
    .winner-box {{
      background: rgba(16, 185, 129, 0.1);
      border: 1px solid #10b981;
      border-radius: 8px;
      padding: 1rem 1.2rem;
      margin-bottom: 1.25rem;
    }}
    .winner-title {{
      font-size: 1.05rem;
      font-weight: bold;
      color: #6ee7b7;
      margin-bottom: 0.5rem;
    }}
    .winner-details {{
      font-size: 0.76rem;
      line-height: 1.6;
      color: #d1fae5;
      word-break: break-all;
    }}
    code {{
      background: #1e293b;
      color: #38bdf8;
      padding: 0.1rem 0.3rem;
      border-radius: 3px;
    }}
    table {{
      width: 100%;
      border-collapse: collapse;
      font-size: 0.92rem;
    }}
    th, td {{
      border: 1px solid #1f2937;
      padding: 0.65rem 0.85rem;
      text-align: left;
    }}
    th {{
      background: #1e293b;
      color: #38bdf8;
      text-transform: uppercase;
      font-size: 0.78rem;
      letter-spacing: 0.05em;
    }}
    tr:nth-child(even) {{
      background: #0f172a;
    }}
    tr.winner-row {{
      background: rgba(16, 185, 129, 0.18) !important;
      color: #6ee7b7;
      font-weight: bold;
    }}
  </style>
</head>
<body>
  <div class="wrap">
    <h1>Maintenance Challenge Leaderboard</h1>
    <div class="meta">Maintenance Day: <strong>{safe_day}</strong> &bull; Cutoff: <strong>{html.escape(cutoff_display)}</strong> &bull; Total Submissions: <strong>{len(completions)}</strong></div>
    {winner_html}
    <table>
      <thead>
        <tr>
          <th style="width:60px;">#</th>
          <th>Username</th>
          <th>Submitted</th>
        </tr>
      </thead>
      <tbody>
        {table_rows}
      </tbody>
    </table>
  </div>
</body>
</html>"""
        return HTMLResponse(content=html_doc, status_code=200)

    return {
        "maintenance_day": day,
        "cutoff": cutoff_display,
        "strategy_commitment": commitment,
        "winner": winner_info,
        "total_completions": len(completions),
        "completions": completions,
    }


@app.get("/api/leaderboard")
async def get_leaderboard_query(
    day: Optional[str] = Query(
        default=None,
        description="Maintenance day in YYYY-MM-DD format (e.g., 2026-11-03)",
    ),
    maintenance_day: Optional[str] = Query(
        default=None,
        description="Alias for 'day' parameter (e.g., 2026-11-03)",
    ),
    f: str = Query(
        default="json",
        description="Output format: 'json' (default) or 'html' for a simple HTML table",
    ),
):
    target_day = maintenance_day or day or get_default_maint_day()
    return render_leaderboard_response(target_day, f)


@app.get("/api/leaderboard/{maintenance_day}")
async def get_leaderboard_path(
    maintenance_day: str,
    f: str = Query(
        default="json",
        description="Output format: 'json' (default) or 'html' for a simple HTML table",
    ),
):
    return render_leaderboard_response(maintenance_day, f)
