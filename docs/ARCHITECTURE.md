# Architecture

The plugin owns the journal UI, lifecycle checks, focus lease, telemetry snapshot, and HTTP request. FastAPI validates the request, normalizes current/max stats, filters Qdrant results by mode, and builds the grounding prompt. Bedrock returns the assistant answer; the API preserves the `{ answer, sources, model }` contract.

Knowledge ingestion parses gameplay headings, skips instruction sections, creates Titan V2 embeddings, and stores section title, hierarchy, and game mode metadata in Qdrant.
