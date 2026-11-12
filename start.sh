#!/bin/bash
set -e

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"

echo "=================================================="
echo "  🚀 Lancement de HiddenJobs Hunter"
echo "  Marché invisible & Remote Software Engineering"
echo "=================================================="

# Function to clean up background processes on exit
cleanup() {
    echo ""
    echo "Arrêt des services..."
    kill $(jobs -p) 2>/dev/null || true
    exit
}
trap cleanup SIGINT SIGTERM EXIT

echo "1. Démarrage du Backend .NET 8 (http://localhost:5000)..."
cd "$DIR/backend"
dotnet run --launch-profile http &
BACKEND_PID=$!

echo "2. Démarrage du Frontend React Vite (http://localhost:5173)..."
cd "$DIR/frontend"
npm run dev &
FRONTEND_PID=$!

echo ""
echo "=================================================="
echo "  ✅ HiddenJobs Hunter est prêt !"
echo "  👉 Ouvrez votre navigateur sur : http://localhost:5173"
echo "  👉 Documentation API Swagger :   http://localhost:5000/swagger"
echo "=================================================="
echo "Appuyez sur Ctrl+C pour arrêter les deux services."

wait
