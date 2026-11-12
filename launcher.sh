#!/usr/bin/env bash

DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
SCRIPTS="$DIR/scripts"

show_menu() {
    clear
    echo "======================================================"
    echo "       🎯 HIDDENJOBS HUNTER - LAUNCHER"
    echo "  Marché invisible & Remote Software Engineering"
    echo "======================================================"
    echo ""
    echo "Statut actuel :"
    if ss -tulpn | grep -q ':5000 '; then
        echo -e "  Backend (.NET 8)   : \033[0;32m● EN LIGNE\033[0m (http://localhost:5000)"
    else
        echo -e "  Backend (.NET 8)   : \033[0;31m○ ARRÊTÉ\033[0m"
    fi

    if ss -tulpn | grep -q ':5173 '; then
        echo -e "  Frontend (React)   : \033[0;32m● EN LIGNE\033[0m (http://localhost:5173)"
    else
        echo -e "  Frontend (React)   : \033[0;31m○ ARRÊTÉ\033[0m"
    fi
    echo ""
    echo "======================================================"
    echo "  1) 🚀 Démarrer le Back et le Front"
    echo "  2) 🛑 Tout arrêter"
    echo "  3) 🌐 Ouvrir l'application (http://localhost:5173)"
    echo "  4) 📑 Ouvrir Swagger API (http://localhost:5000/swagger)"
    echo "  5) 🔄 Rafraîchir l'état"
    echo "  q) 🚪 Quitter le launcher"
    echo "======================================================"
    echo -n "Votre choix [1-5, q] : "
}

while true; do
    show_menu
    read -r choice
    case "$choice" in
        1)
            echo ""
            "$SCRIPTS/start_services.sh"
            echo ""
            read -r -p "Appuyez sur Entrée pour continuer..." _
            ;;
        2)
            echo ""
            "$SCRIPTS/stop_services.sh"
            echo ""
            read -r -p "Appuyez sur Entrée pour continuer..." _
            ;;
        3)
            echo ""
            echo "Ouverture dans le navigateur..."
            if command -v wslview >/dev/null 2>&1; then
                wslview "http://localhost:5173"
            elif command -v xdg-open >/dev/null 2>&1; then
                xdg-open "http://localhost:5173" 2>/dev/null &
            else
                echo "Veuillez ouvrir : http://localhost:5173"
            fi
            read -r -p "Appuyez sur Entrée pour continuer..." _
            ;;
        4)
            echo ""
            echo "Ouverture de Swagger..."
            if command -v wslview >/dev/null 2>&1; then
                wslview "http://localhost:5000/swagger"
            elif command -v xdg-open >/dev/null 2>&1; then
                xdg-open "http://localhost:5000/swagger" 2>/dev/null &
            else
                echo "Veuillez ouvrir : http://localhost:5000/swagger"
            fi
            read -r -p "Appuyez sur Entrée pour continuer..." _
            ;;
        5)
            # Just loop to refresh
            ;;
        q|Q)
            echo "Au revoir !"
            break
            ;;
        *)
            echo "Choix invalide."
            sleep 1
            ;;
    esac
done
