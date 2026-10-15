# Building a Local AI Companion for Green Hell: From Unity Reverse Engineering to a Working BepInEx DLL

I built **Green Hell AI Companion**, an experimental local AI companion for Green Hell. It combines a read-only Unity/BepInEx plugin with a local FastAPI service, Qdrant retrieval, AWS Bedrock GPT-OSS 20B, and Titan Text Embeddings V2.

The complete project is available on GitHub:

**https://github.com/Asyassin10/green-hell-ai-companion-mod**

## Why start with reverse engineering?

The first step was understanding the installed game without changing its original files. The inspected build is Green Hell on Unity `2021.2.20f1`, Windows x64, using the Mono scripting backend. The main gameplay assembly is `GH_Data\\Managed\\Assembly-CSharp.dll`.

The investigation focused on safe observation points for player state, lifecycle changes, input ownership, and UI integration. The project does not bypass DRM, licensing, or anti-cheat systems, and it does not redistribute proprietary game assemblies or decompiled source.

## The first milestone: prove real game data

Before adding an AI backend, I built a minimal BepInEx plugin and verified read-only access to the local player’s health, energy, and stamina. This was the key engineering milestone: the companion had to read real game state safely before it could provide advice.

The plugin uses lifecycle guards for loading screens, menus, missing player objects, and scene changes. Telemetry is sampled at controlled intervals so the journal does not interfere with gameplay performance.

## The notebook UI

The companion opens with **F8** and displays a private survival journal rather than a generic debug window. It includes live condition readings, persistent conversation history, a text editor, Send and Close buttons, Escape handling, focus ownership while typing, and safe cleanup when the player or scene changes.

The interface is owned by the mod and rendered with Unity IMGUI. The DLL does not replace game assets or modify original assemblies.

## Architecture

```text
Green Hell
   -> BepInEx GreenHellAICompanion.dll
   -> POST http://127.0.0.1:8000/v1/chat
   -> FastAPI validation and stat normalization
   -> Qdrant Green Hell knowledge retrieval
   -> AWS Bedrock GPT-OSS 20B
   -> answer, sources, model
   -> notebook history and local typewriter presentation
```

The game plugin remains lightweight. AWS credentials stay in the backend environment and are never embedded in the DLL.

## Building and installing the DLL

The repository includes C# source under `mod/src`. Build with read-only working copies of the inspected Unity managed assemblies and BepInEx 5 x64 Mono core:

```powershell
.\\mod\\Build.ps1 `
  -GameManagedPath 'C:\\analysis\\managed-copy' `
  -BepInExCorePath 'C:\\analysis\\BepInEx-5.4.23.5\\BepInEx\\core' `
  -BuildDirectory '.\\mod\\.build'
```

The script targets x64, C# 5, and treats warnings as errors. It refuses to write into the Steam installation. The reviewed artifact is `releases\\GreenHellAICompanion.dll`.

To install it manually, launch Green Hell once to initialize BepInEx, exit, then copy the DLL to `Green Hell\\BepInEx\\plugins\\GreenHellAICompanion.dll`. Start the backend before launching the game, load a save, and press F8.

## Backend and RAG pipeline

```powershell
python -m venv .venv
.\\.venv\\Scripts\\Activate.ps1
pip install -r backend\\requirements.txt
Copy-Item .env.example .env
docker compose up -d qdrant
$env:PYTHONPATH = (Get-Location).Path + '\\backend'
python scripts\\ingest_knowledge.py 'C:\\path\\to\\GreenHell_AI_RAG_Full_Knowledge.md'
python -m uvicorn app.main:app --host 127.0.0.1 --port 8000
```

The pipeline splits Markdown by gameplay headings, preserves section and game-mode metadata, embeds chunks with Titan V2, and stores them in Qdrant. Survival, Story, and Spirits of Amazonia guidance is filtered by mode.

The API contract remains `{ answer, sources, model }`. Player percentages are computed deterministically before the model is called, so stamina `52/64` becomes `81.25%`.

## Preventing reasoning leakage

Reasoning models can return hidden reasoning separately from the visible answer or use escaped XML-like tags. The backend excludes reasoning and thinking blocks, decodes escaped tags, removes reasoning nodes structurally, and returns only final assistant text to the DLL.

Regression tests cover reasoning extraction, normalized player statistics, grounding rules, mode-aware retrieval, and API compatibility.

## Validation and limitations

The repository contains 11 passing backend tests, a successful Python compile check, a successful x64 C# build, and synthetic C# policy checks. The latest DLL still requires manual in-game validation after installation; source and build verification are kept separate from runtime claims.

AWS credentials must never be committed, logged, placed in the DLL, or pasted into public issues. The repository ignores `.env`, virtual environments, caches, logs, private game files, and local databases. If a credential is exposed, revoke it and issue a replacement before publishing. Bedrock and embedding calls may incur AWS charges.

The next manual test is to open and close the journal with F8, confirm telemetry, submit a grounded Survival question, verify that only the final answer appears, and inspect BepInEx logs.

This project is an example of a practical reverse-engineering workflow: observe safely, prove a small runtime capability, isolate the integration boundary, test each layer independently, and publish only shareable code and artifacts.

**Author:** Yassine Ait Sidi Brahim

**Hashtags:** #ReverseEngineering #Unity #CSharp #BepInEx #GameModding #FastAPI #AWSBedrock #RAG #Qdrant #Python #AIEngineering #GreenHell
