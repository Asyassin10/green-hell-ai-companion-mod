import json
from app.bedrock import BedrockService
from app.config import get_settings
from app.rag import RagStore


QUESTIONS = [
    ("What is a safe way to get drinking water?", "survival", ("hydration", "survival", "water")),
    ("What is the early Story objective?", "story", ("story", "mia")),
    ("What should I do first in Spirits of Amazonia?", "spirits of amazonia", ("spirits", "boat", "elder")),
    ("How do I approach crafting and building?", "survival", ("craft", "build", "construction")),
]

settings = get_settings()
store = RagStore(settings, BedrockService(settings))
report = []
for question, mode, expected in QUESTIONS:
    hits = store.search(question, mode=mode)
    titles = [str((hit.payload or {}).get("section_title", "")) for hit in hits]
    joined = " ".join(titles).lower()
    report.append({"question": question, "mode": mode, "top_k": len(hits),
                   "sections": titles, "matched_expected_term": any(term in joined for term in expected),
                   "scores": [hit.score for hit in hits]})
print(json.dumps(report, indent=2))
