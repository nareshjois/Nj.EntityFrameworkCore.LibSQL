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
| Nelknet.LibSQL.Data | Soft-fork submodule @ `8b5a289` (`main`; upstream `0.2.11` + patches; see [versions.md](versions.md)) |
| Provider version | `10.0.0-preview.1` (in-repo; `UseLibSql` available — not published to NuGet.org yet) |
| Spec suite status | **Active** — `ComplianceTests` hosts G6–G8 suites; see [wp-10-handoff](wp-10-handoff.md) |
| Last completed gate | **WP-10** G6–G8 compliance harness + functional deferred closure |

## Capability matrix (Preview 1)

| Capability | Local | Remote (`sqld` / Turso) | Embedded replica |
|------------|-------|-------------------------|------------------|
| Connect via Nelknet connection string | Working | Working | Preview 2+ |
| Type mapping / parameter round-trips | Working (WP-05) | Working (WP-05) | Preview 2+ |
| Store-generated keys (`INSERT…RETURNING`) | Working (soft-fork) | Working (soft-fork) | Preview 2+ |
| Full LINQ query surface | Compliance hosted (G6) + functional matrix | Compliance hosted (G6) + functional matrix | Preview 2+ |
| Transactions / savepoints | Compliance hosted (G7) + functional deferred | Compliance hosted (G7) + functional deferred | Preview 2+ |
| Migrations | Compliance hosted (G8) + functional deferred | Compliance hosted (G8) + functional deferred | Preview 2+ |
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
| C-002 | Updates / keys | `INSERT…RETURNING` / store-generated ints under `SaveChanges` | **Resolved (soft-fork @ `8b5a289`, upstream `0.2.11`)** — reader drain / statement ownership; HTTP Hrana errors/baton; unprefixed param normalize for `FromSqlInterpolated` (ADR-0001). Stock NuGet may still lag soft-fork-only patches. | — | — |
| C-003 | Scaffolding | CLR type inference via `typeof(max(...))` sampling | Remote/sqld may fail sampling with “database disk image is malformed”; factory logs warning and continues without inferred CLR types. Catalog + CREATE SQL facets still work. | — | — |
| C-004 | Scaffolding | Virtual tables / libSQL vector types | Preview 1 does not reverse-engineer `CREATE VIRTUAL TABLE` (FTS, rtree, vector indexes, etc.) and does not ship first-class FLOAT32/vector CLR mappings. Named SpatiaLite/vector catalog tables remain denylisted. | — | — |
| C-005 | Migrations | Local `EnsureDeleted` / `File.Delete` on Windows | **Mitigated** — soft-fork Close finalizes commands + `ClearPool`; when OS delete remains blocked, provider wipes schema and tombstones the path so `Exists()` is false (file may linger until process exit). | — | — |
| C-006 | Compliance | Spatial / NetTopology spec suites | Not hosted; spatial types out of Preview 1 scope. | — | provider |
| C-007 | Compliance | Stored procedures / UDF DbFunction suites | Not supported; suites not hosted. | — | provider |
| C-008 | Compliance | Cross-connection dirty reads (`Query[_Async]_uses_explicit_transaction`) | **Documented limit** — libSQL does not support SQLite shared-cache mode, so a second connection cannot see another connection's uncommitted writes even with `PRAGMA read_uncommitted` (set on `BeginTransaction(ReadUncommitted)` for ADO.NET parity). `TransactionLibSqlTest` sets `DirtyReadsOccur => false`. Same-connection `UseTransaction` paths pass. | — | provider |
| C-009 | Compliance | `StringTranslationsLibSqlTest` SQL goldens | libSQL quoting / collation differs from Microsoft.Data.Sqlite baselines; functional coverage in WP-06 matrix. | — | provider |
| C-010 | Updates | `UseSqlReturningClause(false)` + AFTER INSERT triggers | Legacy insert path may raise `DbUpdateConcurrencyException`; functional test documents edge case. | — | provider |
| C-011 | Compliance | `GraphUpdates` 1:1 / discriminator / composite-FK edge cases | **Resolved (soft-fork)** — (1) `Read()`/`ExecuteReader` prefetch surfaces UNIQUE on inserts EF never reads; (2) constraint text → `LibSQLConstraintException`; (3) `ExecuteNonQuery` runs full SQL batches so `HasData` seeds all rows (Sqlite parity). | — | provider |
| C-012 | Compliance | Unhosted EF relational `*TestBase` suites | Waived via `C-AUTO` rows in `docs/provider-capabilities.json` until hosted. | — | provider |
| C-014 | Compliance | `OptimisticConcurrencyLibSqlTest` offline-lock resolve/delete paths | Sqlite-parity skip (EF Sqlite / EF Core `#2195`): no DB `rowversion` or auto token bump, so second writers do not raise `DbUpdateConcurrencyException`. Duplicate-insert / M2M association cases run (UNIQUE via soft-fork). | — | provider |
| C-015 | Compliance | Inheritance `Setting_foreign_key_to_a_different_type_throws` | **Resolved** — FK type-mismatch cases now raise `DbUpdateException` after soft-fork constraint surfacing (prefetch + `LibSQLConstraintException` mapping). | — | provider |
| C-016 | Compliance | `BuiltInDataTypesRemoteLibSqlTest` (remote / sqld) | Remote-only compliance slice; excluded from local CI gate. Shared temporal/BLOB fixes apply; remaining remote deltas (HTTP 400 max-length, type coercion over Hrana) tracked in `integration.yml` with `continue-on-error`. | — | provider |

## How to add a waiver

1. Open an issue describing the incompatibility (root cause in Nelknet / libSQL /
   intentional product limit).
2. Add a row to the table above with a stable ID (`C-001`, …).
3. Link the issue from the skip / `Assert.Fail` site in code and in the PR.
4. Prefer upstreaming Nelknet fixes over provider workarounds that violate
   ADO.NET or EF contracts.
