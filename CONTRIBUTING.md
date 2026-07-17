# Contributing to Nj.EntityFrameworkCore.LibSql

Thanks for your interest in contributing. This repository is a community EF Core
10 provider for libSQL. Please read this guide before opening a pull request.

## Code of conduct

Participation is governed by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) matching [`global.json`](global.json)
- Docker Desktop (or compatible engine) for remote `sqld` integration tests
- Optional: `dotnet tool restore` for local `dotnet-ef`

Clone:

```bash
git clone https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL.git
```

The ADO.NET driver lives in-repo (`src/Nj.LibSql.Data` / `Nj.LibSql.Bindings`;
[ADR-0002](docs/adr/0002-nj-libsql-data.md)). No git submodule is required.

Install the local format gate (once per clone):

```bash
./eng/githooks/install
```

This sets `core.hooksPath` to `eng/githooks`. The `pre-commit` hook auto-formats and
verifies C# the same way Ubuntu CI does (`dotnet format --verify-no-changes`, excluding
the EF provider tree and `external/`), so whitespace/usings fail before push.

Before pushing driver / Data / Bindings changes, run the full local CI mirror:

```bash
./eng/verify-driver-ci-local.sh
# Turso job locally (optional):
# export LIBSQL_TEST_URL=… LIBSQL_TEST_AUTH_TOKEN=…
```

See [`docs/adr/0001-soft-fork-nelknet.md`](docs/adr/0001-soft-fork-nelknet.md).

## Development workflow

1. Open an issue for non-trivial design changes before investing large effort.
2. Branch from `main` (or the current integration branch named in the issue).
3. Keep commits focused. When touching EF SQLite baseline source:
   - **Upstream import**, **mechanical rename**, and **LibSQL behavior changes**
     must remain in separate commits.
4. Do not skip specification or contract tests to make CI green. Every permanent
   exclusion needs an entry in [`docs/compatibility.md`](docs/compatibility.md)
   with rationale and an issue link.
5. Prefer small PRs that leave the tree green.

### Local verification

```bash
dotnet restore Nj.EntityFrameworkCore.LibSql.slnx
dotnet format Nj.EntityFrameworkCore.LibSql.slnx --verify-no-changes --exclude ./src/Nj.EntityFrameworkCore.LibSql/** --exclude ./external/**
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.UnitTests -c Release
dotnet test test/Nj.LibSql.DriverContractTests -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release \
  --filter "FullyQualifiedName~LibSqlComplianceTest|FullyQualifiedName~UpdatesLibSqlTest"
./eng/generate-compliance-report.sh || true
./eng/verify-package.sh
```

Remote `sqld` (integration):

```bash
./eng/start-sqld.sh
./eng/wait-for-sqld.sh
export LIBSQL_TEST_URL=http://127.0.0.1:8080
dotnet test test/Nj.LibSql.DriverContractTests -c Release
```

More detail: [`docs/testing.md`](docs/testing.md).

## Design constraints (do not break these)

- Package / namespace / assembly: `Nj.EntityFrameworkCore.LibSql` (Microsoft
  `Sqlite` naming pattern — product name compressed as `LibSql`, not `LibSQL`).
- Public entry points mirror EF Sqlite style: `UseLibSql`,
  `AddEntityFrameworkLibSql`, etc.
- Connection modes are selected via the Nelknet connection string (or an existing
  `LibSQLConnection`). Do not add `UseLibSqlLocal` / `UseLibSqlRemote` /
  `UseLibSqlReplica` helpers.
- Do not reference `Microsoft.EntityFrameworkCore.Sqlite` (it pulls
  `Microsoft.Data.Sqlite` and the native SQLite bundle).
- Do not copy third-party provider implementations (for example BMDRM). Use them
  only as references.
- Do not expose Turso / `sqld` administration (create database, tokens, backups)
  through `DatabaseFacade`.
- Remote / embedded-replica `EnsureDeleted` must throw `NotSupportedException`.

See [`docs/limitations.md`](docs/limitations.md) and
[`docs/provider-development.md`](docs/provider-development.md).

## Pull requests

Use the PR template. Include:

- What changed and why
- How you tested (commands / CI)
- Compatibility impact, if any (`docs/compatibility.md`)
- Connection-mode impact (local / remote / embedded replica), if any

## Licensing

Contributions are accepted under the MIT License ([LICENSE](LICENSE)). By
submitting a pull request you agree that your contribution may be distributed
under that license. There is no CLA at this time.

Preserve Microsoft copyright headers on imported EF Core SQLite source files.
Community modifications must remain clearly attributable; see [NOTICE](NOTICE).

## Security

Do not open public issues for vulnerabilities. Follow [SECURITY.md](SECURITY.md).
