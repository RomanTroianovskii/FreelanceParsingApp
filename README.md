# FreelanceParsingApp

MVP: парсер фриланс-сайтов (FL.ru, Kwork), AI‑фильтрация, Telegram бот, C# UI.

Python: basic parser package is under `python/freelance_parser`.

Запуск (пример):

```bash
python -m freelance_parser.cli fetch --site flru --pages 1
```

Данные сохраняются в `python/freelance_parser/data` в `offers.jsonl` и в SQLite DB `offers.db`.