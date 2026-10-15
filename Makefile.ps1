param([Parameter(Mandatory=$true)][ValidateSet('install','qdrant-up','qdrant-down','qdrant-status','logs','ingest','evaluate','api','test','clean')][string]$Target)
$ErrorActionPreference = 'Stop'
Set-Location $PSScriptRoot
$env:PYTHONPATH = Join-Path $PSScriptRoot 'backend'
$pythonExe = if (Test-Path '.\.venv\Scripts\python.exe') { (Resolve-Path '.\.venv\Scripts\python.exe').Path } else { 'python' }
switch ($Target) {
  'install' { python -m venv .venv; .\.venv\Scripts\python.exe -m pip install -r .\backend\requirements.txt }
  'qdrant-up' { docker compose up -d qdrant }
  'qdrant-down' { docker compose down }
  'qdrant-status' { docker compose ps qdrant; Invoke-RestMethod http://localhost:6333/collections | ConvertTo-Json -Depth 4 }
  'logs' { docker compose logs -f --tail=100 qdrant }
  'ingest' { if (-not $env:QDRANT_COLLECTION) { $env:QDRANT_COLLECTION = 'knowledge' }; & $pythonExe .\scripts\ingest_knowledge.py 'C:\Users\G\Downloads\GreenHell_AI_RAG_Full_Knowledge.md' }
  'evaluate' { if (-not $env:QDRANT_COLLECTION) { $env:QDRANT_COLLECTION = 'knowledge' }; & $pythonExe .\scripts\evaluate_retrieval.py }
  'api' { & $pythonExe -m uvicorn app.main:app --host 127.0.0.1 --port 8000 }
  'test' { & $pythonExe -m pytest -q .\backend\tests }
  'clean' { docker compose down -v }
}
