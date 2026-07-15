# Changelog

All notable changes to `Nj.EntityFrameworkCore.LibSql` are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/) aligned
with EF Core major/minor lines.

## [Unreleased]

### Changed

- Soft-fork [nareshjois/Nelknet.LibSQL](https://github.com/nareshjois/Nelknet.LibSQL)
  as git submodule `external/Nelknet.LibSQL` (ProjectReference; ADR-0001) at
  `@b0a9c51`. Fixes `INSERT…RETURNING` / generated-key `SaveChanges` (`C-002`),
  remote HTTP Hrana error surfacing + baton-backed transactions, and unprefixed
  parameter names for EF `FromSqlInterpolated`.
- Type-mapping round-trips use store-generated integer keys (no longer
  `ValueGeneratedNever`).
- Scaffolding reads COLLATE / AUTOINCREMENT from `sqlite_master` CREATE SQL
  (replaces deferred `sqlite3_table_column_metadata`). Fix embedded
  `LibSqlStrings` resource name so scaffolding logs resolve.
- **C-001:** rewrite decimal LINQ (`ef_*` / `EF_DECIMAL`) to REAL/`CAST`;
  translate `Regex.IsMatch` to native libSQL `REGEXP` (PCRE2). Document
  precision / regex-engine differences in `docs/udf-gap.md`.

### Added

- WP-09 G9: MigrationsSample (Blog/Post + design-time factory + InitialCreate),
  `eng/verify-migrations-sample.sh` CLI smoke, reverse-engineer / migration
  script / virtual-table goldens, and `docs/wp-09-handoff.md` (G9 closed).
- WP-09 first-slice scaffolding FunctionalTests matrix (local + remote
  `IDatabaseModelFactory` catalog cases + design DI / `UseLibSql` codegen)
  and initial `docs/wp-09-handoff.md`.
- WP-08 first-slice migrations FunctionalTests matrix (local + remote
  EnsureCreated/Deleted + Migrate) and `docs/wp-08-handoff.md`.
- WP-07 first-slice update / transaction FunctionalTests matrix (local + remote)
  and `docs/wp-07-handoff.md`.
- WP-06 first-slice query translation FunctionalTests matrix (local + remote)
  with thin SQL capture, and `docs/wp-06-handoff.md`.

### Fixed

- Scaffolding skips `CREATE VIRTUAL TABLE` (C-004); CLR type inference tolerates
  remote/sqld `typeof(max(...))` sampling failures (warn and continue; C-003).
- Migration lock acquire under Nelknet: `LibSqlHistoryRepository` no longer
  relies on multi-statement `SELECT changes()` via ExecuteScalar.
- WP-05 type-mapping / SQL generation round-trips (local + remote), Nelknet
  temporal parameter formats, differential tests vs EF SQLite, `HasTables`
  connection open, and `docs/wp-05-handoff.md`.
- WP-04 Nelknet-backed `UseLibSql` / relational connection (local + remote
  `SELECT 1`, DI/factory/pooled factory smoke tests). Dropped temporary
  `Microsoft.Data.Sqlite.Core`. Catalogued Microsoft EF SQLite UDF gaps
  (`docs/udf-gap.md`); decimal paths later rewritten (Unreleased Changed).
- WP-03 attributed EF Core 10.0.10 `EFCore.Sqlite.Core` baseline import, mechanical
  `LibSql` rename, service map / capabilities docs, and upstream-diff tooling.
- WP-02 Nelknet ADO.NET driver contract tests (local + remote via Testcontainers)
  and `docs/driver-contract.md` findings.
- Migrated primary test suites to `xunit.v3` (`3.2.2`); ComplianceTests stays on
  xUnit v2 while EF Specification.Tests depends on it.
- Full Contributor Covenant 2.1 code of conduct and expanded contributing guide.
- Completed governance and docs for connection modes, limitations, compatibility
  waivers, migrations policy, testing, release policy, and NOTICE attribution.

## [10.0.0-preview.1] — unreleased (local scaffold)

### Added

- Repository scaffold: solution layout, central package management, CI workflows,
  `sqld` compose (pinned digest), eng scripts, and pack/install verification.
- Placeholder public surface `LibSqlProviderInfo` for package smoke tests.
- Initial MIT license, security policy, and documentation stubs (now expanded).

[Unreleased]: https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/compare/main...HEAD
[10.0.0-preview.1]: https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/releases/tag/v10.0.0-preview.1
