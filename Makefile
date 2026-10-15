SHELL := pwsh.exe
.SHELLFLAGS := -NoProfile -NonInteractive -Command

PYTHON ?= python
PROJECT_ROOT := $(CURDIR)
PYTHONPATH_VALUE := $(PROJECT_ROOT)\backend
KNOWLEDGE_DOC ?= C:\Users\G\Downloads\GreenHell_AI_RAG_Full_Knowledge.md

.PHONY: help install qdrant-up qdrant-down qdrant-status logs ingest evaluate api test clean

help:
	Write-Host "Green Hell Backend Agent"; Write-Host "  make install       Create venv and install dependencies"; Write-Host "  make qdrant-up     Start Qdrant in Docker"; Write-Host "  make qdrant-down   Stop Qdrant"; Write-Host "  make qdrant-status Check Qdrant"; Write-Host "  make ingest        Embed knowledge into Qdrant"; Write-Host "  make api           Start FastAPI"; Write-Host "  make test          Run tests"

install:
	$(PYTHON) -m venv .venv; .\.venv\Scripts\python.exe -m pip install -r .\backend\requirements.txt

qdrant-up:
	docker compose up -d qdrant

qdrant-down:
	docker compose down

qdrant-status:
	docker compose ps qdrant; Invoke-RestMethod http://localhost:6333/collections | ConvertTo-Json -Depth 4

logs:
	docker compose logs -f --tail=100 qdrant

ingest:
	$$env:PYTHONPATH = '$(PYTHONPATH_VALUE)'; $$env:QDRANT_COLLECTION = if ($$env:QDRANT_COLLECTION) { $$env:QDRANT_COLLECTION } else { 'knowledge' }; .\.venv\Scripts\python.exe .\scripts\ingest_knowledge.py '$(KNOWLEDGE_DOC)'

evaluate:
	$$env:PYTHONPATH = '$(PYTHONPATH_VALUE)'; $$env:QDRANT_COLLECTION = if ($$env:QDRANT_COLLECTION) { $$env:QDRANT_COLLECTION } else { 'knowledge' }; .\.venv\Scripts\python.exe .\scripts\evaluate_retrieval.py

api:
	$$env:PYTHONPATH = '$(PYTHONPATH_VALUE)'; .\.venv\Scripts\python.exe -m uvicorn app.main:app --host 127.0.0.1 --port 8000

test:
	$$env:PYTHONPATH = '$(PYTHONPATH_VALUE)'; .\.venv\Scripts\python.exe -m pytest -q .\backend\tests

clean:
	docker compose down -v
