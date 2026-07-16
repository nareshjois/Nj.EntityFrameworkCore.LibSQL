# Compatibility

This document is the **capability and waiver manifest** for
`Nj.EntityFrameworkCore.LibSql`. It records intentional differences from EF Core
SQLite behavior, unsupported features, and permanent test exclusions.

Agents and contributors **must not** mark specification or contract tests skipped
without adding a row below (rationale + tracked issue).

## Status

| Item | Value |
|------|--------|
| EF Core target | 10.0.10 |
| Nelknet.LibSQL.Data | Soft-fork submodule @ `aeaecfb` (upstream `0.2.10` + patches; see [versions.md](versions.md)) |
| Provider version | `10.0.0-preview.1` (in-repo; `UseLibSql` available — not published to NuGet.org yet) |
| Spec suite status | Host project present; fixtures land with compliance work (WP-10) |
| Last completed gate | WP-09 G9 (MigrationsSample + scaffold goldens + CLI smoke); full G6–G8 still WP-10 |

## Capability matrix (Preview 1)

| Capability | Local | Remote (`sqld` / Turso) | Embedded replica |
|------------|-------|-------------------------|------------------|
| Connect via Nelknet connection string | Working | Working | Preview 2+ |
| Type mapping / parameter round-trips | Working (WP-05) | Working (WP-05) | Preview 2+ |
| Store-generated keys (`INSERT…RETURNING`) | Working (soft-fork) | Working (soft-fork) | Preview 2+ |
| Full LINQ query surface | Matrix (WP-06 slice); full G6 = WP-10 | Matrix (WP-06 slice); full G6 = WP-10 | Preview 2+ |
| Transactions / savepoints | Matrix (WP-07 slice); savepoints/stress deferred | Matrix (WP-07 slice); savepoints/stress deferred | Preview 2+ |
| Migrations | Matrix (WP-08 slice); locking stress / full G8 deferred | Matrix (WP-08 slice); locking stress / full G8 deferred | Preview 2+ |
| Reverse engineering / design-time DI | Working (WP-09 G9: factory + CLI + goldens) | Working (CLR inference may warn+skip on sqld; C-003) | Preview 2+ |
| Virtual tables / vector types (scaffold) | **Skipped** (C-004) | **Skipped** (C-004) | Preview 2+ |
| `EnsureCreated` (schema in addressable DB) | Working | Working (schema only; no remote admin) | Preview 2+ |
| `EnsureDeleted` | Working (may delete file) | **Not supported** (throws) | **Not supported** (throws) |
| DatabaseFacade sync API | N/A | N/A | Preview 2+ |

## Waiver / exclusion log

No permanent exclusions yet.

| ID | Area | Test / feature | Reason | Issue | Owner |
|----|------|----------------|--------|-------|-------|
| C-001 | Query / UDFs | decimal REAL rewrite; `Regex.IsMatch` → libSQL `REGEXP` (PCRE2) | Not a hard skip: intentional dialect differences vs Microsoft EF SQLite (`ef_*` exact decimal / .NET Regex UDF). See [udf-gap.md](udf-gap.md). | — | — |
| C-002 | Updates / keys | `INSERT…RETURNING` / store-generated ints under `SaveChanges` | **Resolved (soft-fork `main` @ `aeaecfb`)** — reader drain; HTTP Hrana errors/baton; unprefixed param normalize for `FromSqlInterpolated` (ADR-0001). Stock NuGet still needs a separate upstream PR. | — | — |
| C-003 | Scaffolding | CLR type inference via `typeof(max(...))` sampling | Remote/sqld may fail sampling with “database disk image is malformed”; factory logs warning and continues without inferred CLR types. Catalog + CREATE SQL facets still work. | — | — |
| C-004 | Scaffolding | Virtual tables / libSQL vector types | Preview 1 does not reverse-engineer `CREATE VIRTUAL TABLE` (FTS, rtree, vector indexes, etc.) and does not ship first-class FLOAT32/vector CLR mappings. Named SpatiaLite/vector catalog tables remain denylisted. | — | — |
| C-005 | Migrations | Local `EnsureDeleted` / `File.Delete` on Windows | **Mitigated** — soft-fork Close finalizes commands + `ClearPool`; when OS delete remains blocked, provider wipes schema and tombstones the path so `Exists()` is false (file may linger until process exit). | — | — |

## How to add a waiver

1. Open an issue describing the incompatibility (root cause in Nelknet / libSQL /
   intentional product limit).
2. Add a row to the table above with a stable ID (`C-001`, …).
3. Link the issue from the skip / `Assert.Fail` site in code and in the PR.
4. Prefer upstreaming Nelknet fixes over provider workarounds that violate
   ADO.NET or EF contracts.
