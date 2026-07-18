#!/usr/bin/env bash
# PublishAot smoke for samples/AotLocalSample (dynamic natives).
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

if [[ $# -ge 1 && -n "${1:-}" ]]; then
  RID="$1"
else
  RID="$(dotnet --info | awk -F': ' '/RID:/{gsub(/^[[:space:]]+|[[:space:]]+$/, "", $2); print $2; exit}')"
fi
if [[ -z "$RID" ]]; then
  echo "Could not detect RID; pass explicitly: ./eng/smoke-aot.sh osx-arm64" >&2
  exit 1
fi

OUT="$ROOT/artifacts/aot-smoke/$RID"
mkdir -p "$OUT"

echo "==> PublishAot AotLocalSample ($RID)"
dotnet publish samples/AotLocalSample/AotLocalSample.csproj \
  -c Release \
  -r "$RID" \
  -o "$OUT" \
  -p:PublishAot=true \
  -p:TrimMode=partial

echo "==> Run"
"$OUT/AotLocalSample"

echo "AOT smoke succeeded ($RID)."
