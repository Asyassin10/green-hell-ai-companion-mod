import argparse
from pathlib import Path
from app.bedrock import BedrockService
from app.config import get_settings
from app.rag import RagStore

parser = argparse.ArgumentParser(description="Embed a Green Hell Markdown corpus into Qdrant")
parser.add_argument("document", type=Path)
args = parser.parse_args()
settings = get_settings()
count = RagStore(settings, BedrockService(settings)).ingest(args.document)
print(f"Indexed {count} chunks into {settings.qdrant_collection}")
