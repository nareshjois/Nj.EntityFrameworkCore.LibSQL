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
| Nelknet.LibSQL.Data | Soft-fork submodule @ `01a8f52` (upstream `0.2.10` + patches; see [versions.md](versions.md)) |
| Provider version | `10.0.0-preview.1` (in-repo; `UseLibSql` available — not published to NuGet.org yet) |
| Spec suite status | Host project present; fixtures land with compliance work (WP-10) |
| Last completed gate | G5 / WP-05 (type mapping); next WP-06 (query translation) |

## Capability matrix (Preview 1)

| Capability | Local | Remote (`sqld` / Turso) | Embedded replica |
|------------|-------|-------------------------|------------------|
| Connect via Nelknet connection string | Working | Working | Preview 2+ |
| Type mapping / parameter round-trips | Working (WP-05) | Working (WP-05) | Preview 2+ |
| Store-generated keys (`INSERT…RETURNING`) | Working (soft-fork) | Working (soft-fork) | Preview 2+ |
| Full LINQ query surface | Partial — WP-06 | Partial — WP-06 | Preview 2+ |
| Transactions / savepoints | Soft-fork HTTP baton; full suite WP-07 | Soft-fork HTTP baton; full suite WP-07 | Preview 2+ |
| Migrations | WP-08 | WP-08 | Preview 2+ |
| `EnsureCreated` (schema in addressable DB) | Working | Working (schema only; no remote admin) | Preview 2+ |
| `EnsureDeleted` | Working (may delete file) | **Not supported** (throws) | **Not supported** (throws) |
| DatabaseFacade sync API | N/A | N/A | Preview 2+ |

## Waiver / exclusion log

No permanent exclusions yet.

| ID | Area | Test / feature | Reason | Issue | Owner |
|----|------|----------------|--------|-------|-------|
| C-001 | Query / UDFs | `ef_*` decimal helpers, `EF_DECIMAL`, `regexp` | Nelknet lacks CreateFunction/CreateAggregate/CreateCollation; fail at translation per [udf-gap.md](udf-gap.md) | TBD | — |
| C-002 | Updates / keys | `INSERT…RETURNING` / store-generated ints under `SaveChanges` | **Resolved (soft-fork `main` @ `01a8f52`)** — reader drain to `SQLITE_DONE` on `Close`; HTTP Hrana top-level errors + baton streams (ADR-0001). Stock NuGet still needs a separate upstream PR. Covered by type-mapping + `GeneratedKeySaveChangesTests`. | — | — |

## How to add a waiver

1. Open an issue describing the incompatibility (root cause in Nelknet / libSQL /
   intentional product limit).
2. Add a row to the table above with a stable ID (`C-001`, …).
3. Link the issue from the skip / `Assert.Fail` site in code and in the PR.
4. Prefer upstreaming Nelknet fixes over provider workarounds that violate
   ADO.NET or EF contracts.
