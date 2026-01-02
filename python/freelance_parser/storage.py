import json
import sqlite3
from pathlib import Path
from typing import Iterator

from .models import Offer

DATA_DIR = Path(__file__).resolve().parents[1] / "data"
DATA_DIR.mkdir(parents=True, exist_ok=True)
JSONL_PATH = DATA_DIR / "offers.jsonl"
SQLITE_PATH = DATA_DIR / "offers.db"


def save_jsonl(offer: Offer):
    with JSONL_PATH.open("a", encoding="utf-8") as f:
        f.write(json.dumps(offer.dict(), default=str, ensure_ascii=False) + "\n")


def iter_jsonl() -> Iterator[Offer]:
    if not JSONL_PATH.exists():
        return
    with JSONL_PATH.open("r", encoding="utf-8") as f:
        for line in f:
            data = json.loads(line)
            yield Offer(**data)


def init_sqlite():
    conn = sqlite3.connect(SQLITE_PATH)
    cur = conn.cursor()
    cur.execute(
        """
        CREATE TABLE IF NOT EXISTS offers (
            id TEXT PRIMARY KEY,
            site TEXT,
            title TEXT,
            url TEXT UNIQUE,
            description TEXT,
            budget TEXT,
            posted_at TEXT,
            scraped_at TEXT
        )
        """
    )
    conn.commit()
    conn.close()


def save_sqlite(offer: Offer):
    conn = sqlite3.connect(SQLITE_PATH)
    cur = conn.cursor()
    cur.execute(
        """
        INSERT OR IGNORE INTO offers (id, site, title, url, description, budget, posted_at, scraped_at)
        VALUES (?, ?, ?, ?, ?, ?, ?, ?)
        """,
        (
            offer.id,
            offer.site,
            offer.title,
            offer.url,
            offer.description,
            offer.budget,
            offer.posted_at,
            str(offer.scraped_at),
        ),
    )
    conn.commit()
    conn.close()
