# Changelog

All notable changes to `Nj.EntityFrameworkCore.LibSql` are documented here.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.1.0/),
and this project adheres to [Semantic Versioning](https://semver.org/) aligned
with EF Core major/minor lines.

## [Unreleased]

## [10.0.0-preview.1] — 2026-07-17

First public prerelease on NuGet.org. APIs may change before stable `10.0.x`.
Known gaps: [compatibility.md](docs/compatibility.md) (including **C-019** Turso
Sync hang).

### Added

- EF Core 10 provider `Nj.EntityFrameworkCore.LibSql` on in-repo
  `Nj.LibSql.Data` / `Nj.LibSql.Bindings` (local natives + HTTP/WSS Hrana).
- Connection modes: local file / `:memory:`, remote `sqld`/Turso HTTP,
  embedded-replica Sync vs self-hosted `sqld` (`Database.Sync`).
- Migrations, scaffolding, updates/transactions, query translation matrices;
  ComplianceTests harness; DriverContractTests; ConnectionModes suites.
- Platform Preview: advertised RIDs `linux-x64`, `osx-arm64`, `win-x64`; pack /
  publish / container smoke; soft BenchmarkDotNet baselines.
- Observability/security: connection-string redaction, HTTP-first cancellation,
  `LibSqlActivitySource`, CodeQL + dependency-review + gitleaks + SBOM +
  provenance on pack.
- Samples: Local, Remote, EmbeddedReplica, DiPooling, Migrations.
- Docs: connection modes/strings, migrate-from-EF-Sqlite, transactions,
  deployment, releasing runbook, CONTRIBUTING G13 path.

### Known limitations

- Turso Cloud embedded-replica Sync hangs on pinned natives (**C-019** / #24).
- Extra RIDs / musl / NativeAOT not advertised (stable backlog).
- Decimal LINQ → REAL; `Regex.IsMatch` → PCRE2 `REGEXP` (C-001).

[Unreleased]: https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/compare/v10.0.0-preview.1...HEAD
[10.0.0-preview.1]: https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL/releases/tag/v10.0.0-preview.1
