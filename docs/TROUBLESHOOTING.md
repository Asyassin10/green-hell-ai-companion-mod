# Troubleshooting

Check `/health` first. If Qdrant is unavailable, run `docker compose up -d qdrant`. If Bedrock calls fail, verify region, model access, and rotated credentials. If the notebook does not open, inspect `BepInEx\LogOutput.log`, confirm the DLL is in `BepInEx\plugins`, and restart the game after replacing the DLL. Do not bypass loader, licensing, or anti-cheat controls.
