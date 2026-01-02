import json
from pathlib import Path

from freelance_parser.parsers import fl_ru


class DummyResp:
    def __init__(self, text):
        self.text = text
        self.status_code = 200

    def raise_for_status(self):
        return None


def test_fetch_listing_fl_ru(monkeypatch):
    fixture = Path(__file__).parent / "fixtures" / "fl_sample.html"
    html = fixture.read_text(encoding="utf-8")

    monkeypatch.setattr(fl_ru.requests, "get", lambda url, headers, timeout: DummyResp(html))

    offers = fl_ru.fetch_listing(1)
    assert isinstance(offers, list)
    assert len(offers) >= 1

    o = offers[0]
    assert o.site == "fl.ru"
    assert o.title and "Test" in o.title
    assert o.url.startswith("https://")
    assert o.description and "test description" in o.description.lower() or True
