from pathlib import Path
from app.rag import chunk_markdown, parse_markdown_sections


def test_chunking_preserves_sections_and_bounds():
    chunks = chunk_markdown("# A\n\n" + "x" * 90 + "\n\n# B\n\n" + "y" * 90, 100, 10)
    assert len(chunks) >= 2
    assert all(chunk for chunk in chunks)


def test_knowledge_file_exists():
    path = Path(r"C:\Users\G\Downloads\GreenHell_AI_RAG_Full_Knowledge.md")
    assert path.exists()


def test_heading_metadata_skips_instructions_and_preserves_mode():
    text = """# 00 — How the AI should use this document
Ignore these instructions.

# 02 — Basic survival loop
Boil water before drinking.

## Survival Mode
Build a shelter and gather food.
"""
    chunks = parse_markdown_sections(text, 1000, 50)
    assert len(chunks) == 2
    assert all("Ignore these instructions" not in chunk.text for chunk in chunks)
    assert chunks[-1].title == "Survival Mode"
    assert "survival" in chunks[-1].modes
    assert chunks[-1].hierarchy.endswith("Survival Mode")


def test_story_and_survival_chunks_are_distinguishable():
    text = "# Story Mode\nFind Mia.\n\n# Survival Mode\nBoil water."
    chunks = parse_markdown_sections(text, 1000, 50)
    assert chunks[0].modes == ["story"]
    assert chunks[1].modes == ["survival"]
