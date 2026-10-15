from app.main import build_system_prompt
from app.models import GameState


def test_stats_are_normalized_before_prompt():
    state = GameState(stamina=52, max_stamina=64, energy=64, max_energy=100, health=78, max_health=100)
    normalized = state.normalized()
    assert normalized["stamina_percent"] == 81.25
    assert normalized["energy_percent"] == 64.0
    assert normalized["health_percent"] == 78.0


def test_water_grounding_and_mode_separation_are_explicit():
    prompt = build_system_prompt(GameState(mode="Survival"), "Boil untreated water to reduce parasite risk.")
    assert "Never claim water is safe because it looks clear" in prompt
    assert "Do not mix Story, Survival, and Spirits of Amazonia" in prompt
    assert "Boil untreated water" in prompt
