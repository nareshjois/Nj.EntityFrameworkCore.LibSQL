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
| Nelknet.LibSQL.Data | Soft-fork submodule @ `b0a9c51` (upstream `0.2.10` + patches; see [versions.md](versions.md)) |
| Provider version | `10.0.0-preview.1` (in-repo; `UseLibSql` available — not published to NuGet.org yet) |
| Spec suite status | Host project present; fixtures land with compliance work (WP-10) |
| Last completed gate | WP-09 first slice (FunctionalTests scaffolding matrix); full G6–G9 still WP-10 / later |

## Capability matrix (Preview 1)

| Capability | Local | Remote (`sqld` / Turso) | Embedded replica |
|------------|-------|-------------------------|------------------|
| Connect via Nelknet connection string | Working | Working | Preview 2+ |
| Type mapping / parameter round-trips | Working (WP-05) | Working (WP-05) | Preview 2+ |
| Store-generated keys (`INSERT…RETURNING`) | Working (soft-fork) | Working (soft-fork) | Preview 2+ |
| Full LINQ query surface | Matrix (WP-06 slice); full G6 = WP-10 | Matrix (WP-06 slice); full G6 = WP-10 | Preview 2+ |
| Transactions / savepoints | Matrix (WP-07 slice); savepoints/stress deferred | Matrix (WP-07 slice); savepoints/stress deferred | Preview 2+ |
| Migrations | Matrix (WP-08 slice); locking stress / full G8 deferred | Matrix (WP-08 slice); locking stress / full G8 deferred | Preview 2+ |
| Reverse engineering / design-time DI | Matrix (WP-09 slice); CLI goldens / full G9 deferred | Matrix (WP-09 slice); CLR inference may warn+skip on sqld | Preview 2+ |
| `EnsureCreated` (schema in addressable DB) | Working | Working (schema only; no remote admin) | Preview 2+ |
| `EnsureDeleted` | Working (may delete file) | **Not supported** (throws) | **Not supported** (throws) |
| DatabaseFacade sync API | N/A | N/A | Preview 2+ |

## Waiver / exclusion log

No permanent exclusions yet.

| ID | Area | Test / feature | Reason | Issue | Owner |
|----|------|----------------|--------|-------|-------|
| C-001 | Query / UDFs | decimal REAL rewrite; `Regex.IsMatch` → libSQL `REGEXP` (PCRE2) | Not a hard skip: intentional dialect differences vs Microsoft EF SQLite (`ef_*` exact decimal / .NET Regex UDF). See [udf-gap.md](udf-gap.md). | — | — |
| C-002 | Updates / keys | `INSERT…RETURNING` / store-generated ints under `SaveChanges` | **Resolved (soft-fork `main` @ `b0a9c51`)** — reader drain; HTTP Hrana errors/baton; unprefixed param normalize for `FromSqlInterpolated` (ADR-0001). Stock NuGet still needs a separate upstream PR. | — | — |
| C-003 | Scaffolding | CLR type inference via `typeof(max(...))` sampling | Remote/sqld may fail sampling with “database disk image is malformed”; factory logs warning and continues without inferred CLR types. Catalog + CREATE SQL facets still work. | — | — |

## How to add a waiver

1. Open an issue describing the incompatibility (root cause in Nelknet / libSQL /
   intentional product limit).
2. Add a row to the table above with a stable ID (`C-001`, …).
3. Link the issue from the skip / `Assert.Fail` site in code and in the PR.
4. Prefer upstreaming Nelknet fixes over provider workarounds that violate
   ADO.NET or EF contracts.
