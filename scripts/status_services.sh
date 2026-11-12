#!/usr/bin/env bash

BACKEND="OFF"
FRONTEND="OFF"

if ss -tulpn | grep -q ':5000 '; then
    BACKEND="ON"
fi

if ss -tulpn | grep -q ':5173 '; then
    FRONTEND="ON"
fi

echo "BACKEND=$BACKEND"
echo "FRONTEND=$FRONTEND"
