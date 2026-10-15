import re
from dataclasses import dataclass
from pathlib import Path
from typing import List, Optional
from qdrant_client import QdrantClient, models

SKIP_SECTION_MARKERS = ("how the ai should use", "rag ingestion", "system instructions", "ingestion instructions")


@dataclass
class MarkdownChunk:
    text: str
    title: str
    hierarchy: str
    modes: List[str]


def _modes(title: str, text: str) -> List[str]:
    combined = (title + "\n" + text).lower()
    found = set()
    if "story mode" in combined or "story-specific" in combined:
        found.add("story")
    if "survival mode" in combined or "survival" in combined:
        found.add("survival")
    if "spirits of amazonia" in combined or re.search(r"\bsoa\b", combined):
        found.add("spirits of amazonia")
    return sorted(found) or ["all"]


def parse_markdown_sections(text: str, size: int, overlap: int) -> List[MarkdownChunk]:
    if size <= 0 or overlap < 0 or overlap >= size:
        raise ValueError("chunk size must be positive and overlap must be smaller than size")
    sections = []
    headings: List[str] = []
    blocks: List[str] = []
    current_title = "Document"
    for line in text.splitlines():
        match = re.match(r"^(#{1,6})\s+(.+?)\s*$", line)
        if match:
            if blocks:
                sections.append((current_title, " > ".join(headings), "\n".join(blocks).strip()))
                blocks = []
            level = len(match.group(1))
            title = match.group(2).strip().strip("#").strip()
            headings = headings[: max(0, level - 1)]
            headings.append(title)
            current_title = title
        else:
            blocks.append(line)
    if blocks:
        sections.append((current_title, " > ".join(headings), "\n".join(blocks).strip()))

    chunks: List[MarkdownChunk] = []
    for title, hierarchy, section_text in sections:
        if not section_text or any(marker in title.lower() for marker in SKIP_SECTION_MARKERS):
            continue
        paragraphs = [p.strip() for p in re.split(r"\n\s*\n", section_text) if p.strip()]
        current = ""
        for paragraph in paragraphs:
            candidate = (current + "\n\n" + paragraph).strip()
            if current and len(candidate) > size:
                chunks.append(MarkdownChunk(current, title, hierarchy, _modes(title, current)))
                current = ((current[-overlap:] if overlap else "") + "\n\n" + paragraph).strip()
            else:
                current = candidate
        if current:
            chunks.append(MarkdownChunk(current, title, hierarchy, _modes(title, current)))
    return chunks


def chunk_markdown(text: str, size: int, overlap: int) -> List[str]:
    return [chunk.text for chunk in parse_markdown_sections(text, size, overlap)]


class RagStore:
    def __init__(self, settings, bedrock):
        self.settings = settings
        self.bedrock = bedrock
        self.client = QdrantClient(url=settings.qdrant_url, timeout=settings.request_timeout_seconds)

    def ensure_collection(self):
        names = {item.name for item in self.client.get_collections().collections}
        if self.settings.qdrant_collection not in names:
            self.client.create_collection(collection_name=self.settings.qdrant_collection,
                vectors_config=models.VectorParams(size=self.settings.embedding_dimensions, distance=models.Distance.COSINE))

    def ingest(self, document: Path, replace: bool = True) -> int:
        if replace:
            names = {item.name for item in self.client.get_collections().collections}
            if self.settings.qdrant_collection in names:
                self.client.delete_collection(self.settings.qdrant_collection)
        self.ensure_collection()
        chunks = parse_markdown_sections(document.read_text(encoding="utf-8"), self.settings.rag_chunk_size, self.settings.rag_chunk_overlap)
        points = []
        for index, chunk in enumerate(chunks):
            points.append(models.PointStruct(id=index, vector=self.bedrock.embed(chunk.text), payload={
                "text": chunk.text, "source": document.name, "chunk": index,
                "section_title": chunk.title, "hierarchy": chunk.hierarchy, "modes": chunk.modes}))
        if points:
            self.client.upsert(collection_name=self.settings.qdrant_collection, points=points, wait=True)
        return len(points)

    def search(self, query: str, mode: Optional[str] = None):
        self.ensure_collection()
        vector = self.bedrock.embed(query)
        result = self.client.query_points(collection_name=self.settings.qdrant_collection,
            query=vector, limit=max(self.settings.rag_top_k * 3, self.settings.rag_top_k), with_payload=True)
        points = result.points
        if mode:
            requested = mode.lower().strip()
            if requested == "soa": requested = "spirits of amazonia"
            points = [point for point in points if requested in (point.payload or {}).get("modes", ["all"])
                      or "all" in (point.payload or {}).get("modes", ["all"])]
        return points[: self.settings.rag_top_k]
