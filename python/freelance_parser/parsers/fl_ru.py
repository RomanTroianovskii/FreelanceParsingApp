import time
import re
import requests
from bs4 import BeautifulSoup
from urllib.parse import urljoin
from typing import List

from ..models import Offer

BASE_URL = "https://www.fl.ru"
LISTING_URL = "https://www.fl.ru/projects/"
HEADERS = {
    "User-Agent": "Mozilla/5.0 (compatible; FreelanceParser/0.1; +https://example.com)"
}


def fetch_listing(page: int = 1) -> List[Offer]:
    """Fetches a single listing page from FL.ru and returns a list of Offer objects (basic fields).

    This implementation uses heuristic parsing and should be refined with site-specific selectors.
    """
    url = f"{LISTING_URL}?page={page}"
    resp = requests.get(url, headers=HEADERS, timeout=10)
    resp.raise_for_status()

    soup = BeautifulSoup(resp.text, "lxml")
    offers = []

    # Prefer parsing project cards (elements with class that includes 'b-post')
    posts = soup.find_all("div", class_=lambda c: c and "b-post" in c)
    for p in posts:
        # Title and URL
        title_tag = p.find("h2", class_=lambda c: c and "b-post__title" in c)
        if not title_tag:
            continue
        a = title_tag.find("a", href=True)
        if not a:
            continue
        title = a.get_text(strip=True)
        href = a["href"]
        # Only accept proper project links like /projects/<id>/...
        if not re.search(r"/projects/\d+/", href):
            continue
        full_url = urljoin(BASE_URL, href)

        # Description
        desc_tag = p.find("div", class_=lambda c: c and "b-post__txt text-5" in c)
        description = desc_tag.get_text(strip=True) if desc_tag else None

        # Budget/price
        price_tag = p.find("div", class_=lambda c: c and "b-post__price" in c)
        budget = None
        if price_tag:
            budget = " ".join(price_tag.stripped_strings)

        offer = Offer(
            site="fl.ru",
            title=title,
            url=full_url,
            description=description,
            budget=budget,
        )
        offers.append(offer)

    # Simple deduplication by URL
    unique = {}
    for o in offers:
        unique[o.url] = o

    time.sleep(1)
    return list(unique.values())


if __name__ == "__main__":
    found = fetch_listing(1)
    print(f"Found {len(found)} offers on FL.ru (page 1)")
    for o in found[:10]:
        print(o.title, o.url)
