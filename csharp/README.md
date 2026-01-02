# FreelanceViewer (C# Avalonia app)

Simple cross-platform desktop app to view offers saved by Python parsers.

Prerequisites:
- .NET SDK 8.0+ installed
- (optional) `dotnet` Avalonia templates

Run (from `csharp/FreelanceViewer`):

```bash
dotnet restore
dotnet run
```

App reads SQLite DB located at `python/data/offers.db` relative to repo root. Use Refresh to reload data, Search to filter by title, Open URL to open selected offer in the system browser.
