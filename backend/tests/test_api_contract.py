from types import SimpleNamespace
from fastapi.testclient import TestClient
import app.main as main


def test_chat_contract_and_normalized_prompt(monkeypatch):
    hit = SimpleNamespace(score=0.87, payload={"section_title": "Hydration", "text": "Boil water.", "modes": ["survival"]})
    monkeypatch.setattr(main.rag, "search", lambda query, mode=None: [hit])
    captured = {}

    def complete(system, messages, **kwargs):
        captured["system"] = system
        return "Boil collected water before drinking."

    monkeypatch.setattr(main.bedrock, "complete", complete)
    response = TestClient(main.app).post("/v1/chat", json={
        "message": "What should I drink?",
        "state": {"stamina": 52, "max_stamina": 64, "mode": "Survival"},
        "history": []})
    assert response.status_code == 200
    payload = response.json()
    assert set(payload) == {"answer", "sources", "model"}
    assert payload["sources"][0]["section"] == "Hydration"
    assert "stamina_percent': 81.25" in captured["system"]
