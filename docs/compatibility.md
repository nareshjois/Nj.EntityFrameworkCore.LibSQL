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
| Nelknet.LibSQL.Data | 0.2.10 |
| Provider version | 10.0.0-preview.1 (scaffold; `UseLibSql` not shipped yet) |
| Spec suite status | Host project present; fixtures land with compliance work |

## Capability matrix (Preview 1 intent)

| Capability | Local | Remote (`sqld` / Turso) | Embedded replica |
|------------|-------|-------------------------|------------------|
| Connect via Nelknet connection string | Planned | Planned | Preview 2+ |
| LINQ / change tracking / SaveChanges | Planned | Planned | Preview 2+ |
| Transactions / savepoints | Planned (per Nelknet) | Planned (per Nelknet) | Preview 2+ |
| Migrations | Planned | Planned | Preview 2+ |
| `EnsureCreated` (schema in addressable DB) | Planned | Planned | Preview 2+ |
| `EnsureDeleted` | Planned (may delete file) | **Not supported** (throws) | **Not supported** (throws) |
| DatabaseFacade sync API | N/A | N/A | Preview 2+ |

## Waiver / exclusion log

No permanent exclusions yet.

| ID | Area | Test / feature | Reason | Issue | Owner |
|----|------|----------------|--------|-------|-------|
| C-001 | Query / UDFs | `ef_*` decimal helpers, `EF_DECIMAL`, `regexp` | Nelknet lacks CreateFunction/CreateAggregate/CreateCollation; fail at translation per [udf-gap.md](udf-gap.md) | TBD | — |
| C-002 | Updates / keys | `INSERT…RETURNING` / store-generated ints under `SaveChanges` | **Resolved (local/native)** via soft-fork Nelknet: drain reader to `SQLITE_DONE` on `Close` ([nelknet#99](https://github.com/nelknet/Nelknet.LibSQL/pull/99), ADR-0001). Type-mapping round-trips still use `ValueGeneratedNever` to keep remote HTTP saves independent of RETURNING. | — | — |

## How to add a waiver

1. Open an issue describing the incompatibility (root cause in Nelknet / libSQL /
   intentional product limit).
2. Add a row to the table above with a stable ID (`C-001`, …).
3. Link the issue from the skip / `Assert.Fail` site in code and in the PR.
4. Prefer upstreaming Nelknet fixes over provider workarounds that violate
   ADO.NET or EF contracts.
