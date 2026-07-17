# ADR 0002: Clean-rewrite `Nj.LibSql.Data` ADO.NET driver

## Status

**Implemented** (Phase 3 cutover) — 2026-07-17. Supersedes
[ADR-0001](0001-soft-fork-nelknet.md).

Accepted — 2026-07-16 (design); soft-fork remained EF default until cutover.

## Context

The EF provider currently depends on a soft-fork of `Nelknet.LibSQL.Data`
([ADR-0001](0001-soft-fork-nelknet.md)). Soft-fork types use `LibSQL*` naming and
HTTP-only Hrana. We need a first-party driver that:

- Matches Microsoft-style `LibSql*` naming (same brand as `Nj.EntityFrameworkCore.LibSql`)
- Ships HTTP **and** WebSocket Hrana (large results on Turso)
- Owns native packaging without committing Nelknet’s managed codebase

Work proceeds **in parallel** with the soft-fork so existing EF gates stay green.

## Decision

1. **Clean rewrite** of managed ADO.NET under `src/Nj.LibSql.Data/` (not a rename
   of Nelknet sources). Soft-fork semantics are a **requirements checklist** only.
2. **Packages:** `Nj.LibSql.Data` + `Nj.LibSql.Bindings` (Data ProjectReferences Bindings).
3. **Public types:** `LibSqlConnection`, `LibSqlCommand`, `LibSqlDataReader`,
   `LibSqlParameter`, `LibSqlParameterCollection`, `LibSqlTransaction`,
   `LibSqlFactory`, `LibSqlConnectionStringBuilder`, exceptions as needed.
   Namespace `Nj.LibSql.Data` / `Nj.LibSql.Bindings`.
4. **Natives:** DuckDB.NET-style MSBuild download from **our** GitHub Release
   artifacts (FFI client libs). RIDs: `linux-x64`, `osx-arm64`, `win-x64` only.
   Official [libsql releases](https://github.com/tursodatabase/libsql/releases)
   publish libsql-server, not the C client library — do not download those for P/Invoke.
5. **Cutover gate:** EF ProjectReference swap + `LibSQL`→`LibSql` rename only after
   local + Testcontainers sqld + Turso WSS (including large-result) DriverContract
   are green. No HTTP-only cutover.
6. **CI:** Path-filtered `.github/workflows/libsql-driver.yml` for Data/Bindings/driver
   tests only. EF CI unchanged until Phase 3. Turso secrets required when that
   workflow’s Turso job runs (Phase 2+).
7. **Phase 0 skip policy:** Mirrored DriverContract facts use
   `[PendingDriverFact]` until Phase 1 (local) / Phase 2 (remote) implement them.
   Scaffold smoke tests (construct types) may run unskipped.

## Consequences

- Soft-fork submodule removed at Phase 3; EF uses `Nj.LibSql.Data` only.
- Never dual-reference soft-fork and `Nj.LibSql.Data`.
- Connection-string keys stay Turso-compatible.
- Embedded replica: accept CS keys; sync deferred to Preview 2.
- WP-11 and soft-fork work must not be blocked by this track.
- **Native bump:** run `.github/workflows/libsql-native.yml`, update
  `LibSqlNativeVersion` / `docs/versions.md`, and prefer publishing
  `native-libsql-v*` release assets for `BuildType=Full` downloads
  (see [eng/native/README.md](../eng/native/README.md)).
