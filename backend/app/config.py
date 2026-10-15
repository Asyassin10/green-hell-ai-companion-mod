from functools import lru_cache
import os
from pathlib import Path
from pydantic import BaseModel, Field
from dotenv import load_dotenv

load_dotenv(Path(__file__).resolve().parents[2] / ".env")


class Settings(BaseModel):
    aws_region: str = Field(default="us-east-1", alias="AWS_REGION")
    bedrock_model_id: str = Field(default="openai.gpt-oss-20b-1:0", alias="BEDROCK_MODEL_ID")
    bedrock_embedding_model_id: str = Field(default="amazon.titan-embed-text-v2:0", alias="BEDROCK_EMBEDDING_MODEL_ID")
    embedding_dimensions: int = Field(default=1024, alias="EMBEDDING_DIMENSIONS")
    bedrock_guardrail_id: str = Field(default="", alias="BEDROCK_GUARDRAIL_ID")
    bedrock_guardrail_version: str = Field(default="1", alias="BEDROCK_GUARDRAIL_VERSION")
    qdrant_url: str = Field(default="http://localhost:6333", alias="QDRANT_URL")
    qdrant_collection: str = Field(default="green-hell", alias="QDRANT_COLLECTION")
    rag_top_k: int = Field(default=5, alias="RAG_TOP_K")
    rag_chunk_size: int = Field(default=1000, alias="RAG_CHUNK_SIZE")
    rag_chunk_overlap: int = Field(default=150, alias="RAG_CHUNK_OVERLAP")
    api_host: str = Field(default="127.0.0.1", alias="API_HOST")
    api_port: int = Field(default=8000, alias="API_PORT")
    request_timeout_seconds: float = Field(default=45.0, alias="REQUEST_TIMEOUT_SECONDS")

    @classmethod
    def from_env(cls) -> "Settings":
        values = {field.alias: os.environ[field.alias] for field in cls.model_fields.values() if field.alias in os.environ}
        return cls.model_validate(values)


@lru_cache(maxsize=1)
def get_settings() -> Settings:
    return Settings.from_env()
