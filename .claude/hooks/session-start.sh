#!/usr/bin/env bash
set -u

# 1. Docker-status
if ! docker info >/dev/null 2>&1; then
    echo "⚠ Docker körs inte. Starta Docker Desktop för att kunna köra tester."
elif ! docker compose ps --format json 2>/dev/null | grep -q '"State":"running"'; then
    echo "ℹ Docker Compose-tjänster är nere. Kör 'docker compose up -d' för dev-miljön."
else
    echo "✓ Docker Compose-tjänster uppe."
fi

# 2. .env-fil
if [ ! -f .env ]; then
    echo "⚠ .env saknas i repo-roten. Kopiera från .env.example om du inte redan gjort det."
else
    echo "✓ .env finns."
fi

# 3. Uncommitted changes från förra sessionen
if ! git diff --quiet 2>/dev/null || ! git diff --cached --quiet 2>/dev/null; then
    echo "⚠ Oparsade ändringar finns från förra sessionen — kolla 'git status' innan du börjar."
    git status --short | head -10
fi

# 4. current-work.md
if [ -f docs/current-work.md ]; then
    echo ""
    echo "== docs/current-work.md (senaste session) =="
    head -40 docs/current-work.md
else
    echo "ℹ docs/current-work.md finns inte än — skapas vid första /session-end."
fi

exit 0
