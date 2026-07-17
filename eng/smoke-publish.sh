#!/usr/bin/env bash
# WP-12: framework-dependent, self-contained, and single-file publish smoke for LocalSample.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

RID="${1:-$(dotnet --info | awk '/RID:/{print $2; exit}')}"
OUT="${ROOT}/artifacts/publish-smoke/${RID}"
rm -rf "$OUT"
mkdir -p "$OUT"

echo "==> Framework-dependent publish (LocalSample)"
dotnet publish samples/LocalSample/LocalSample.csproj -c Release -o "$OUT/fdd" --nologo -v q
"$OUT/fdd/LocalSample"

echo "==> Self-contained publish (RID=${RID})"
dotnet publish samples/LocalSample/LocalSample.csproj \
  -c Release -r "$RID" --self-contained true \
  -o "$OUT/sc" --nologo -v q
"$OUT/sc/LocalSample"

echo "==> Single-file self-contained publish (RID=${RID})"
dotnet publish samples/LocalSample/LocalSample.csproj \
  -c Release -r "$RID" --self-contained true -p:PublishSingleFile=true \
  -o "$OUT/sf" --nologo -v q
"$OUT/sf/LocalSample"

echo "Publish smoke succeeded for RID=${RID}."
