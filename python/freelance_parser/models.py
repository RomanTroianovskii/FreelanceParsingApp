from pydantic import BaseModel, Field
from datetime import datetime
from typing import Optional
from uuid import uuid4


class Offer(BaseModel):
    id: str = Field(default_factory=lambda: str(uuid4()))
    site: str
    title: str
    url: str
    description: Optional[str] = None
    budget: Optional[str] = None
    posted_at: Optional[str] = None
    scraped_at: datetime = Field(default_factory=datetime.utcnow)
