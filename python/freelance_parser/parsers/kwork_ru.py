import time
import requests
from bs4 import BeautifulSoup
from typing import List
from ..models import Offer

BASE_URL = "https://kwork.ru"
LISTING_URL = BASE_URL + "/projects"
HEADERS = {"User-Agent": "Mozilla/5.0 (compatible; FreelanceParser/0.1; +https://example.com)"}


def fetch_listing(page: int = 1) -> List[Offer]:
    """Fetches a single page of projects from Kwork using their JSON endpoint.

    The frontend posts to /projects and receives a JSON with 'data.wants' array.
    """
    resp = requests.post(LISTING_URL, data={"page": page}, headers=HEADERS, timeout=10)
    resp.raise_for_status()

    data = resp.json().get("data", {})
    wants = data.get("wants", []) if data else []

    offers: List[Offer] = []

    for w in wants:
        wid = w.get("id")
        if not wid:
            continue
        title = w.get("name") or (w.get("description") or "").strip().split("\n")[0][:200]
        description = w.get("description")
        # Some descriptions contain HTML entities / tags - clean via BeautifulSoup
        if description:
            description = BeautifulSoup(description, "lxml").get_text(separator=" \n").strip()

        # price: prefer possiblePriceLimit, fall back to priceLimit
        budget = w.get("possiblePriceLimit") or w.get("priceLimit") or None

        posted_at = w.get("date_create")

        url = f"{BASE_URL}/projects/{wid}"

        offers.append(
            Offer(
                site="kwork.ru",
                title=title,
                url=url,
                description=description,
                budget=str(budget) if budget is not None else None,
                posted_at=posted_at,
            )
        )

    time.sleep(1)
    return offers


if __name__ == "__main__":
    found = fetch_listing(1)
    print(f"Found {len(found)} offers on Kwork (page 1)")
    for o in found[:10]:
        print(o.title, o.url)
