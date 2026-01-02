import json
from pathlib import Path

from freelance_parser.parsers import kwork_ru


class DummyResp:
    def __init__(self, json_data):
        self._json = json_data
        self.status_code = 200

    def raise_for_status(self):
        return None

    def json(self):
        return self._json


def test_fetch_listing_kwork(monkeypatch):
    fixture = Path(__file__).parent / "fixtures" / "kwork_sample.json"
    data_fixture = json.loads(fixture.read_text(encoding="utf-8"))

    # Return the fixture JSON regardless of POST payload
    monkeypatch.setattr(kwork_ru.requests, "post", lambda url, data, headers, timeout: DummyResp(data_fixture))

    offers = kwork_ru.fetch_listing(1)
    assert isinstance(offers, list)
    assert len(offers) == 1

    o = offers[0]
    assert o.site == "kwork.ru"
    assert o.url.startswith("https://kwork.ru/projects/")
    assert o.budget is not None
