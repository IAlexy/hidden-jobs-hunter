#!/usr/bin/env bash
set -e

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
mkdir -p "$DIR/logs"

# Ensure environment variables for .NET and Node.js
export NVM_DIR="$HOME/.nvm"
[ -s "$NVM_DIR/nvm.sh" ] && \. "$NVM_DIR/nvm.sh"
export DOTNET_ROOT="$HOME/.dotnet"
export DOTNET_SYSTEM_GLOBALIZATION_INVARIANT=1
export PATH="$HOME/.dotnet:$HOME/.local/bin:$PATH"

echo "=========================================="
echo "  Démarrage de HiddenJobs Hunter"
echo "=========================================="

# 1. Backend
if ss -tulpn | grep -q ':5000 '; then
    echo "🟢 Backend déjà en cours d'exécution sur le port 5000."
else
    echo "⏳ Lancement du Backend .NET 8..."
    cd "$DIR/backend"
    setsid dotnet run --launch-profile http </dev/null > "$DIR/logs/backend.log" 2>&1 &
    PID=$!
    echo $PID > "$DIR/logs/backend.pid"
    echo "🚀 Backend démarré (PID: $PID)."
fi

# 2. Frontend
if ss -tulpn | grep -q ':5173 '; then
    echo "🟢 Frontend déjà en cours d'exécution sur le port 5173."
else
    echo "⏳ Lancement du Frontend React Vite..."
    cd "$DIR/frontend"
    setsid npm run dev </dev/null > "$DIR/logs/frontend.log" 2>&1 &
    PID=$!
    echo $PID > "$DIR/logs/frontend.pid"
    echo "🚀 Frontend démarré (PID: $PID)."
fi

echo "=========================================="
echo "  Vérification dans 3 secondes..."
sleep 3

if ss -tulpn | grep -q ':5000 '; then
    echo "  Backend  : OK (http://localhost:5000)"
else
    echo "  Backend  : Démarrage en cours (consulter logs/backend.log)"
fi

if ss -tulpn | grep -q ':5173 '; then
    echo "  Frontend : OK (http://localhost:5173)"
else
    echo "  Frontend : Démarrage en cours (consulter logs/frontend.log)"
fi
echo "=========================================="
