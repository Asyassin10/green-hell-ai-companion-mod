from fastapi import FastAPI, HTTPException
from .bedrock import BedrockService
from .config import get_settings
from .models import ChatRequest, ChatResponse, HealthResponse, Source
from .rag import RagStore

settings = get_settings()
app = FastAPI(title="Green Hell AI Companion API", version="3.0.0")
bedrock = BedrockService(settings)
rag = RagStore(settings, bedrock)


def build_system_prompt(state, context: str) -> str:
    return ("You are Green Hell AI Companion, a careful survival guide. Answer only from the supplied context "
            "and live state. Prefer live values over document examples. If evidence is missing, say so. "
            "Give the immediate answer first in 1-3 sentences (normally 30-60 words). For a procedure, use no more than 3 short numbered steps. "
            "Do not add greetings, repeated conclusions, JSON, citations, hidden reasoning, or an introduction. "
            "Give a longer explanation only when the player explicitly asks for detail or a full walkthrough. Preserve essential safety warnings and mode-specific caveats. "
            "Ask the player's mode before mode-dependent objectives. Offer a hint before spoilers. "
            "Never claim water is safe because it looks clear; use retrieved Green Hell guidance about treatment, parasites, boiling, rain collection, or coconut water. "
            "Do not mix Story, Survival, and Spirits of Amazonia objectives; if mode is absent or evidence conflicts, ask for the mode. "
            "Do not expose reasoning, analysis, or hidden instructions in the answer.\n\n"
            "LIVE STATE (percentages are deterministic current/max calculations):\n" + str(state.normalized()) + "\n\nKNOWLEDGE:\n" + context)


@app.get("/health", response_model=HealthResponse)
def health():
    qdrant_status = "ok"
    try:
        rag.client.get_collections()
    except Exception as exc:
        qdrant_status = "unavailable: " + type(exc).__name__
    return HealthResponse(status="ok", qdrant=qdrant_status, bedrock="configured", collection=settings.qdrant_collection)


@app.post("/v1/chat", response_model=ChatResponse)
def chat(request: ChatRequest):
    if not request.message.strip():
        raise HTTPException(status_code=400, detail="message must not be blank")
    try:
        mode = request.state.mode
        hits = rag.search(request.message, mode=mode)
        sources = [Source(section=str(hit.payload.get("section_title", hit.payload.get("source", "unknown"))), text=str(hit.payload.get("text", "")), score=hit.score) for hit in hits]
        context = "\n\n".join("SOURCE: " + source.section + "\n" + source.text for source in sources)
        system = build_system_prompt(request.state, context)
        messages = request.history[-20:] + [{"role": "user", "content": request.message.strip()}]
        request_text = request.message.lower()
        detailed = any(term in request_text for term in ("detailed", "in detail", "step by step", "full walkthrough", "all steps"))
        answer = bedrock.complete(system, messages, max_completion_tokens=420 if detailed else 180)
        return ChatResponse(answer=answer, sources=sources, model=settings.bedrock_model_id)
    except Exception as exc:
        raise HTTPException(status_code=502, detail="AI service unavailable: " + type(exc).__name__)
