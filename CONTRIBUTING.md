# Contributing to Nj.EntityFrameworkCore.LibSql

Thanks for your interest. This is a community EF Core 10 provider for libSQL.
Please read this guide before opening a pull request.

## Code of conduct

Participation is governed by [CODE_OF_CONDUCT.md](CODE_OF_CONDUCT.md).

## Prerequisites

- [.NET 10 SDK](https://dotnet.microsoft.com/download) matching [`global.json`](global.json)
- Docker Desktop (or compatible) for remote `sqld` tests
- Optional: `dotnet tool restore` for local `dotnet-ef`

```bash
git clone https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL.git
```

The ADO.NET driver is in-repo (`src/Nj.LibSql.Data` / `Nj.LibSql.Bindings`;
[docs/architecture.md](docs/architecture.md)).

Install the format gate once per clone:

```bash
./eng/githooks/install
```

This sets `core.hooksPath` to `eng/githooks`. The hook matches Ubuntu CI
(`dotnet format --verify-no-changes`, excluding the imported EF baseline tree).

Before pushing driver / Data / Bindings changes:

```bash
./eng/verify-driver-ci-local.sh
# Optional Turso: export LIBSQL_TEST_URL=… LIBSQL_TEST_AUTH_TOKEN=…
```

## Development workflow

1. Open an issue for non-trivial design changes before large effort.
2. Branch from `main`.
3. Keep commits focused. When touching EF SQLite baseline source:
   - **Upstream import**, **mechanical rename**, and **libSQL behavior changes**
     must stay in separate commits ([upstream-baseline.md](docs/upstream-baseline.md)).
4. Do not skip specification or contract tests to go green. Permanent exclusions
   need a row in [compatibility.md](docs/compatibility.md) with rationale and an issue.
5. Prefer small PRs that leave the tree green.

### Local verification

```bash
dotnet restore Nj.EntityFrameworkCore.LibSql.slnx
dotnet format Nj.EntityFrameworkCore.LibSql.slnx --verify-no-changes \
  --exclude ./src/Nj.EntityFrameworkCore.LibSql/**
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.UnitTests -c Release
dotnet test test/Nj.LibSql.DriverContractTests -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release \
  --filter "FullyQualifiedName~LibSqlComplianceTest|FullyQualifiedName~UpdatesLibSqlTest"
./eng/generate-compliance-report.sh || true
./eng/verify-package.sh
```

More detail: [docs/testing.md](docs/testing.md).

## Design constraints

- Package / namespace / assembly: `Nj.EntityFrameworkCore.LibSql` (`LibSql`, not `LibSQL`).
- Public entry points mirror EF Sqlite: `UseLibSql`, `AddEntityFrameworkLibSql`, etc.
- Connection modes via `Nj.LibSql.Data` connection string (or existing
  `LibSqlConnection`). No `UseLibSqlLocal` / `UseLibSqlRemote` / `UseLibSqlReplica`.
- Do not reference `Microsoft.EntityFrameworkCore.Sqlite`.
- Do not copy third-party provider implementations.
- Do not expose Turso / `sqld` administration through `DatabaseFacade`.
- Remote / embedded-replica `EnsureDeleted` must throw `NotSupportedException`.

See [limitations.md](docs/limitations.md) and [architecture.md](docs/architecture.md).

## Pull requests

Include what changed and why, how you tested, and any compatibility or
connection-mode impact.

## Licensing

MIT ([LICENSE](LICENSE)). Preserve Microsoft copyright headers on imported
baseline files. See [NOTICE](NOTICE).

## Security

Do not open public issues for vulnerabilities. Follow [SECURITY.md](SECURITY.md).
