#!/usr/bin/env bash
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT/eng/sqld"
docker compose up -d
echo "sqld starting on http://127.0.0.1:8080"
