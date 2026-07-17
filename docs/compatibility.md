# Compatibility

Capability matrix and waiver log for `Nj.EntityFrameworkCore.LibSql`.

Do **not** skip specification or contract tests permanently without adding a row
below (rationale + tracked issue). Cross-vendor notes:
[turso-dotnet-comparison.md](turso-dotnet-comparison.md).

## Status

| Item | Value |
|------|--------|
| EF Core | `10.0.10` |
| ADO.NET | In-repo `Nj.LibSql.Data` / `Nj.LibSql.Bindings` ([versions.md](versions.md)) |
| Provider | `10.0.0-preview.1` |

## Capability matrix (Preview 1)

| Capability | Local file | Remote (`sqld` / Turso) | Embedded replica |
|------------|------------|-------------------------|------------------|
| Connect via connection string | Yes | Yes | Yes (local path + `Sync URL`) |
| Connect via existing `LibSqlConnection` | Yes | Yes | Yes |
| Type mapping / parameter round-trips | Yes | Yes | Yes (after Sync) |
| Store-generated keys (`INSERT…RETURNING`) | Yes | Yes | Yes |
| LINQ / queries | Yes | Yes | Yes |
| CRUD / `SaveChanges` | Yes | Yes | Yes |
| Transactions / SQL savepoints | Yes | Yes | Yes |
| Migrations `Up`/`Down` / `Database.Migrate` | Yes | Yes (schema in existing DB) | Schema on primary; replica via Sync |
| Reverse engineering / design-time | Yes | Yes (CLR sampling may warn; C-003) | Local replica file |
| Virtual tables / vector scaffold | **No** (C-004) | **No** (C-004) | **No** (C-004) |
| `EnsureCreated` | Yes (may create file) | Schema only | Schema on local file / Sync |
| `EnsureDeleted` | Yes (may delete file) | **No** — throws | **No** — throws |
| SpatiaLite / loadable extensions | **No** | **No** | **No** |
| Turso admin (create DB / tokens) | N/A | **Out of scope** | **Out of scope** |
| `Database.Sync` / `LibSqlConnection.Sync` | N/A | N/A | Yes (`sqld` validated; Turso C-019) |

Server-version gated features (e.g. `RETURNING`, some JSON functions) use
`LibSqlDatabaseCapabilities` against the bundled / remote libSQL version — not
`Microsoft.Data.Sqlite`.

## Waiver / exclusion log

| ID | Area | Test / feature | Reason | Issue | Owner |
|----|------|----------------|--------|-------|-------|
| C-001 | Query | Decimal → REAL/`CAST`; `Regex.IsMatch` → libSQL `REGEXP` (PCRE2) | Intentional dialect vs EF SQLite `ef_*` / .NET Regex UDFs. Driver has no `CreateFunction`. See [limitations.md](limitations.md). | — | — |
| C-002 | Updates / keys | `INSERT…RETURNING` under `SaveChanges` | **Resolved** in `Nj.LibSql.Data` — reader drain / statement ownership; HTTP Hrana baton/errors; unprefixed param normalize for `FromSqlInterpolated`. | — | — |
| C-003 | Scaffolding | CLR type inference via `typeof(max(...))` | Remote/sqld may fail sampling (“database disk image is malformed”); factory warns and continues without inferred CLR types. | — | — |
| C-004 | Scaffolding | Virtual tables / vector types | Preview 1 does not reverse-engineer `CREATE VIRTUAL TABLE` or ship FLOAT32/vector CLR mappings. | — | — |
| C-005 | Migrations | Local `EnsureDeleted` / `File.Delete` on Windows | **Mitigated** — Close finalizes commands + `ClearPool`; if OS delete is blocked, wipe schema and tombstone the path so `Exists()` is false. | — | — |
| C-006 | Compliance | Spatial / NetTopology suites | Not hosted; spatial out of Preview 1. | — | provider |
| C-007 | Compliance | Stored procedures / UDF DbFunction suites | Not supported; suites not hosted. | — | provider |
| C-008 | Compliance | Cross-connection dirty reads | libSQL has no SQLite shared-cache; second connection cannot see uncommitted writes. `DirtyReadsOccur => false`. | — | provider |
| C-009 | Compliance | `StringTranslationsLibSqlTest` SQL goldens | libSQL quoting / collation can differ from Microsoft.Data.Sqlite baselines. | — | provider |
| C-010 | Updates | `UseSqlReturningClause(false)` + AFTER INSERT triggers | Legacy insert path may raise `DbUpdateConcurrencyException`. | — | provider |
| C-011 | Compliance | `GraphUpdates` edge cases | **Resolved** — reader prefetch surfaces UNIQUE; constraint → `LibSqlConstraintException`; full-batch `ExecuteNonQuery` for `HasData`. | — | provider |
| C-012 | Compliance | Unhosted EF relational `*TestBase` suites | Waived via `C-AUTO` in `docs/provider-capabilities.json` until hosted. | — | provider |
| C-014 | Compliance | Optimistic concurrency offline-lock paths | Sqlite-parity skip (EF Core `#2195`): no DB `rowversion`. | — | provider |
| C-015 | Compliance | Inheritance FK type-mismatch throws | **Resolved** — raises `DbUpdateException` after constraint surfacing. | — | provider |
| C-016 | Compliance | `BuiltInDataTypesRemoteLibSqlTest` | Remote-only; excluded from local CI gate. Tracked in `integration.yml`. | — | provider |
| C-018 | Remote HTTP | Large buffered result sets | HTTP Hrana buffers rows in memory; prefer `ws://` against self-hosted sqld for very large cursors. Turso stays on HTTP. | — | driver |
| C-019 | Embedded replica | Turso Cloud `Sync` hang | Pinned native (`libsql-server-v0.24.32`) opens a Turso replica but `libsql_sync2` hangs; Sync is validated on self-hosted `sqld` only. | [#24](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/issues/24) | driver |

## Driver behavior notes

Useful ADO.NET facts validated by `test/Nj.LibSql.DriverContractTests`:

- Prefer named `@p` parameters; unprefixed names normalize to `@`-style for EF.
- SQL `SAVEPOINT` / `ROLLBACK TO` / `RELEASE` works; no first-class savepoint API on `LibSqlTransaction`.
- Enforce connection affinity for `command.Transaction` in the provider (do not rely on the driver alone).
- Some statement failures may surface as `InvalidOperationException` with SQLite text embedded; EF mapping accepts both that and typed `LibSqlException`.

## How to add a waiver

1. Open an issue (root cause in driver / libSQL / intentional product limit).
2. Add a row above with a stable ID (`C-001`, …).
3. Link the issue from the skip site and the PR.
4. Prefer fixes in `Nj.LibSql.Data` over provider workarounds that break ADO.NET/EF contracts.
