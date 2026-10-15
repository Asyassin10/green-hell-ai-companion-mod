from typing import Any, Dict, List, Optional
from pydantic import BaseModel, Field


def _percent(value, maximum):
    if value is None or maximum is None or maximum <= 0:
        return None
    return round(max(0.0, min(100.0, float(value) / float(maximum) * 100.0)), 2)


class GameState(BaseModel):
    health: Optional[float] = None
    max_health: Optional[float] = None
    energy: Optional[float] = None
    max_energy: Optional[float] = None
    stamina: Optional[float] = None
    max_stamina: Optional[float] = None
    hydration: Optional[float] = None
    nutrition: Optional[Dict[str, float]] = None
    sanity: Optional[float] = None
    scene: Optional[str] = None
    mode: Optional[str] = None
    inventory: Optional[List[Dict[str, Any]]] = None
    statuses: Optional[List[str]] = None

    def normalized(self):
        result = self.model_dump()
        result.update({
            "health_percent": _percent(self.health, self.max_health),
            "energy_percent": _percent(self.energy, self.max_energy),
            "stamina_percent": _percent(self.stamina, self.max_stamina),
        })
        return result


class ChatRequest(BaseModel):
    message: str = Field(min_length=1, max_length=4000)
    state: GameState = Field(default_factory=GameState)
    history: List[Dict[str, str]] = Field(default_factory=list, max_length=40)


class Source(BaseModel):
    section: str
    text: str
    score: Optional[float] = None


class ChatResponse(BaseModel):
    answer: str
    sources: List[Source] = Field(default_factory=list)
    model: str


class HealthResponse(BaseModel):
    status: str
    qdrant: str
    bedrock: str
    collection: str
