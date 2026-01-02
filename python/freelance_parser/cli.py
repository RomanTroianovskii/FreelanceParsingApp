import click
from .parsers.fl_ru import fetch_listing
from .storage import save_jsonl, save_sqlite, init_sqlite


@click.group()
def cli():
    pass


@cli.command()
@click.option("--site", type=click.Choice(["flru","kwork"], case_sensitive=False), default="flru")
@click.option("--pages", default=5, help="Number of pages to fetch (default: 5)")
def fetch(site, pages):
    """Fetch listings from a supported site and save to storage"""
    init_sqlite()
    for p in range(1, pages + 1):
        if site.lower() == "flru":
            offers = fetch_listing(p)
        elif site.lower() == "kwork":
            from .parsers.kwork_ru import fetch_listing as fetch_kwork

            offers = fetch_kwork(p)
        else:
            offers = []

        for o in offers:
            save_jsonl(o)
            save_sqlite(o)
    click.echo(f"Saved offers from {site} pages=1..{pages}")


if __name__ == "__main__":
    cli()