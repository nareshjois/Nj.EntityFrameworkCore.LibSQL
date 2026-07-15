#!/usr/bin/env bash
set -euo pipefail
URL="${LIBSQL_TEST_URL:-http://127.0.0.1:8080}"
TIMEOUT_SECONDS="${1:-60}"
echo "Waiting for sqld at ${URL} (timeout ${TIMEOUT_SECONDS}s)..."
deadline=$((SECONDS + TIMEOUT_SECONDS))
while (( SECONDS < deadline )); do
  if curl -fsS "$URL/v2" >/dev/null 2>&1 || curl -fsS "$URL" >/dev/null 2>&1; then
    echo "sqld is reachable."
    exit 0
  fi
  sleep 1
done
echo "Timed out waiting for sqld at ${URL}" >&2
exit 1
