# Performance baselines (WP-12 Preview)

Soft regression baselines for `Nj.EntityFrameworkCore.LibSql` / `Nj.LibSql.Data`
local mode. **Not** a parity promise vs Microsoft EF SQLite.

## How to run

```bash
./eng/run-benchmarks.sh
```

Results land under `artifacts/benchmarks/` (JSON + Markdown from BenchmarkDotNet).
CI uploads them as an artifact; they are **not** a hard fail gate in Preview.

## Scenarios

| Scenario | Notes |
|----------|--------|
| Cold `Open` | Local file |
| `SELECT 1` | Local file |
| EF insert batch (50) | Local LibSql |
| EF short transaction | Local LibSql |
| EF insert batch (50) Sqlite | Differential only |

## Soft threshold

On the **same runner class** (e.g. `ubuntu-latest`), investigate if a scenario’s
mean latency is **more than ~2×** the previous recorded artifact. Do not fail
the build on noise across different machines or architectures.

## Harness notes

BenchmarkDotNet runs with `UnrollFactor=1` and a modest `InvocationCount` so
native opens do not exhaust file handles (`SQLITE_CANTOPEN`). `SELECT 1` reuses
one open connection (pooled-query style). Sqlite insert-batch is differential
only (test project suppresses transitive `NU1903` from `Microsoft.Data.Sqlite`).

## Differential

Sqlite insert-batch numbers are recorded for orientation only. Different engines
and native stacks are expected to diverge; do not treat them as a pass/fail gate.
