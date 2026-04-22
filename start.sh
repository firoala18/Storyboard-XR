#!/usr/bin/env bash
# Automatisches Deployment für die Mathematik‑Didaktik‑WebApp
# Abbruch bei erstem Fehler:
set -euo pipefail

WORK_DIR="./ProjectsWebApp"
SERVICE="storyboard.service"

echo " Wechsel in Arbeitsverzeichnis $WORK_DIR"
cd "$WORK_DIR"

echo " Git Pull (fragt ggf. Benutzername/Passwort)…"
git pull       # Git erledigt die Abfrage automatisch, falls kein Credential‑Helper aktiv ist. :contentReference[oaicite:0]{index=0}

echo " .NET-Projekt bauen …"
sudo dotnet build                                           # :contentReference[oaicite:1]{index=1}

echo " EF-Core-Migrationen anwenden …"
dotnet ef database update                              # :contentReference[oaicite:2]{index=2}

echo " Release-Build publizieren …"
sudo dotnet publish -c Release -o ./publish                 # :contentReference[oaicite:3]{index=3}

echo " systemd-Dienst neu starten …"
sudo systemctl restart "$SERVICE"                           # :contentReference[oaicite:4]{index=4}

echo "⏳  Warte 5-Sekunden, bis der Dienst sauber hochgefahren ist …"
sleep 5                                                     # bei Bedarf anpassen

echo " Dienststatus:"
sudo systemctl status "$SERVICE" --no-pager                 # :contentReference[oaicite:5]{index=5}

echo "✅  Deployment abgeschlossen."