# WP-01 handoff

Branch: `wp-01-repository-scaffold`

## Done

- Solution `Nj.EntityFrameworkCore.LibSql.slnx` with src / test / samples layout.
- Pins: EF Core `10.0.10`, Nelknet `0.2.10`, SDK `10.0.100`, package `10.0.0-preview.1`.
- Placeholder public surface: `LibSqlProviderInfo`.
- CI: `build`, `integration`, `nightly`, `package`, `codeql`.
- `eng/sqld/docker-compose.yml` pinned to `libsql-server:ef758d9@sha256:817fb6c…`.
- Local G1: `./eng/verify-package.sh` packs nupkg/snupkg and installs into PackageTests + LocalSample.

## Deferred

- Package icon asset.
- PublicApiAnalyzers (enable with real `UseLibSql` surface in WP-04).
- PackageValidationBaselineVersion (after first published package).

## Next

- WP-02 driver contract tests and/or WP-03 EF SQLite baseline import (parallel-safe).
