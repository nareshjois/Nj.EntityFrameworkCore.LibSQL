# WP-12 handoff — Platform, packaging, perf (Preview G11)

## Summary

Closed Preview acceptance gate **G11**: advertise and smoke the three shipped
desktop RIDs, packaging/deployment checks, and soft perf baselines.

## Advertised RIDs

| RID | Smoke |
|-----|--------|
| `linux-x64` | CI (`PlatformSmokeTests`, pack layout, publish + container) |
| `win-x64` | CI (`PlatformSmokeTests`, pack layout, publish) |
| `osx-arm64` | Maintainer-validated; committed natives |

**Not advertised:** `win-arm64`, `linux-arm64`, `osx-x64`, musl/Alpine, mobile.

Docs: [architecture.md](architecture.md), [versions.md](versions.md),
[deployment.md](deployment.md).

## Packaging / deploy

- `eng/smoke-publish.sh` — FDD + self-contained (+ single-file probe)
- `eng/smoke-container.sh` + `eng/docker/Dockerfile.local-sample` — `linux/amd64`
  LocalSample / SELECT 1
- Single-file: document support only where native load works on advertised RIDs
  (see deployment.md)

## Perf

- `test/Nj.EntityFrameworkCore.LibSql.Benchmarks` + `eng/run-benchmarks.sh`
- CI uploads `artifacts/benchmarks` (Linux) — **not** a hard fail gate
- Soft threshold: investigate if mean latency **>2×** prior artifact on the
  same runner class — [performance.md](performance.md)
- Sqlite insert-batch differential is informational only

## Explicitly deferred

- Extra desktop RIDs / musl / NativeAOT productization
- Turso Rust-engine ADO/EF driver
- C-019 Turso Cloud Sync hang ([#24](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/issues/24))

## Next

WP-13 (observability/security) → WP-14 docs polish → WP-15 preview NuGet.
