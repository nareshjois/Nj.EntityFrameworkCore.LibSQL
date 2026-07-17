# Changelog

All notable changes to `Nj.EntityFrameworkCore.LibSql` are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/) aligned
with EF Core major/minor lines.

## [Unreleased]

### Changed

- EF provider now uses in-repo `Nj.LibSql.Data` / `Nj.LibSql.Bindings` (HTTP +
  WebSocket Hrana, local natives). Documentation streamlined: architecture,
  connection modes, testing, compatibility, limitations — ADRs and work-package
  handoffs removed.
- Type-mapping round-trips use store-generated integer keys.
- Scaffolding reads COLLATE / AUTOINCREMENT from `sqlite_master` CREATE SQL.
- Decimal LINQ rewritten to REAL/`CAST`; `Regex.IsMatch` → native libSQL
  `REGEXP` (PCRE2). See `docs/limitations.md` and waiver C-001.

### Added

- Migrations sample + `eng/verify-migrations-sample.sh`, reverse-engineer /
  migration script / virtual-table goldens.
- Functional matrices for scaffolding, migrations, updates/transactions, and
  query translation (local + remote).
- Driver contract tests (`Nj.LibSql.DriverContractTests`) with Testcontainers
  sqld + Turso CI jobs.
- Attributed EF Core 10.0.10 SQLite baseline import and upstream-diff tooling.
- xunit.v3 for primary suites; ComplianceTests remains on xUnit v2 for EF Spec.
- Governance docs, Contributor Covenant, pack/install verification.

### Fixed

- Windows `EnsureDeleted` when the native handle stays locked (C-005).
- Scaffolding skips virtual tables (C-004); CLR sampling tolerates remote
  failures (C-003).
- Migration lock acquire uses split commands for reliable `ExecuteScalar`.
- Store-generated keys / RETURNING drain, constraint surfacing, batch
  `ExecuteNonQuery` (C-002 / C-011).

## [10.0.0-preview.1] — unreleased (local scaffold)

### Added

- Repository scaffold: solution layout, central package management, CI workflows,
  `sqld` compose (pinned digest), eng scripts, and pack/install verification.
- Placeholder public surface `LibSqlProviderInfo` for package smoke tests.
- Initial MIT license, security policy, and documentation stubs.

[Unreleased]: https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/compare/main...HEAD
[10.0.0-preview.1]: https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/releases/tag/v10.0.0-preview.1
