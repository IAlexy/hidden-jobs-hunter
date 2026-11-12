#!/usr/bin/env bash

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"

echo "=========================================="
echo "  Arrêt de HiddenJobs Hunter"
echo "=========================================="

# 1. Kill via ports 5000 and 5173
fuser -k 5000/tcp 2>/dev/null || true
fuser -k 5173/tcp 2>/dev/null || true

# 2. Kill by PIDs if saved
if [ -f "$DIR/logs/backend.pid" ]; then
    PID=$(cat "$DIR/logs/backend.pid" 2>/dev/null || true)
    if [ -n "$PID" ]; then
        kill -9 "$PID" 2>/dev/null || true
    fi
    rm -f "$DIR/logs/backend.pid"
fi

if [ -f "$DIR/logs/frontend.pid" ]; then
    PID=$(cat "$DIR/logs/frontend.pid" 2>/dev/null || true)
    if [ -n "$PID" ]; then
        kill -9 "$PID" 2>/dev/null || true
    fi
    rm -f "$DIR/logs/frontend.pid"
fi

# 3. Clean up any lingering dotnet / vite processes related to this project
pkill -f "HiddenJobsHunter.Api" 2>/dev/null || true
pkill -f "vite" 2>/dev/null || true

sleep 1

# 4. Status check
if ss -tulpn | grep -qE ':5000 |:5173 '; then
    echo "⚠️ Certains processus résiduels sont encore actifs."
else
    echo "✅ Tous les services (Backend & Frontend) ont été arrêtés avec succès."
fi
echo "=========================================="
