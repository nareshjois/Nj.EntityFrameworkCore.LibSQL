# Changelog

All notable changes to `Nj.EntityFrameworkCore.LibSql` are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/) aligned
with EF Core major/minor lines.

## [Unreleased]

### Added

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
