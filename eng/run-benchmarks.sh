#!/usr/bin/env bash
# WP-12: run local BenchmarkDotNet baselines; copy results to artifacts.
set -euo pipefail
ROOT="$(cd "$(dirname "${BASH_SOURCE[0]}")/.." && pwd)"
cd "$ROOT"

OUT="$ROOT/artifacts/benchmarks"
mkdir -p "$OUT"

echo "==> BenchmarkDotNet (ShortRun)"
dotnet run --project test/Nj.EntityFrameworkCore.LibSql.Benchmarks/Nj.EntityFrameworkCore.LibSql.Benchmarks.csproj \
  -c Release -- \
  --filter '*' \
  --exporters json markdown

# BenchmarkDotNet writes under BenchmarkDotNet.Artifacts next to the project / cwd.
if [[ -d BenchmarkDotNet.Artifacts ]]; then
  cp -R BenchmarkDotNet.Artifacts/. "$OUT/" || true
fi
if [[ -d test/Nj.EntityFrameworkCore.LibSql.Benchmarks/BenchmarkDotNet.Artifacts ]]; then
  cp -R test/Nj.EntityFrameworkCore.LibSql.Benchmarks/BenchmarkDotNet.Artifacts/. "$OUT/" || true
fi

echo "Benchmark results under $OUT"
echo "Soft threshold: investigate if mean latency regresses by >2x vs prior artifact on the same runner class."
