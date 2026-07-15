# Testing

## Test projects

| Project | Role |
|---------|------|
| `Nj.EntityFrameworkCore.LibSql.UnitTests` | Fast provider unit tests |
| `Nj.EntityFrameworkCore.LibSql.FunctionalTests` | EF + local/remote integration |
| `Nj.EntityFrameworkCore.LibSql.ComplianceTests` | Published EF relational specification suites |
| `Nj.EntityFrameworkCore.LibSql.DriverContractTests` | Nelknet-only ADO.NET contract (no EF) |
| `Nj.EntityFrameworkCore.LibSql.PackageTests` | Pack + clean NuGet install verification |
| `TestUtilities` | Shared helpers (connection strings, env) |

Primary suites use **xUnit v3** (`xunit.v3`, `OutputType=Exe`). `ComplianceTests`
stays on xUnit v2 while EF `Specification.Tests` packages depend on `xunit.core`
2.9.x.

## Commands

```bash
# Format (SDK built-in)
dotnet format Nj.EntityFrameworkCore.LibSql.slnx --verify-no-changes
# Imported EF SQLite baseline and soft-forked Nelknet are excluded from format:
#   --exclude ./src/Nj.EntityFrameworkCore.LibSql/** --exclude ./external/**

# Build
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release

# Unit tests
dotnet test test/Nj.EntityFrameworkCore.LibSql.UnitTests -c Release

# Functional placeholders / integration (expands over time)
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release

# Pack
./eng/pack.sh

# Pack + install package into PackageTests + run LocalSample
./eng/verify-package.sh
```

On Windows, use the corresponding `eng/*.ps1` scripts.

## Remote `sqld`

Driver remote tests start the pinned `libsql-server` image via **Testcontainers**
(Docker required). Override with an external endpoint if needed:

```bash
export LIBSQL_TEST_URL=http://127.0.0.1:8080   # skip Testcontainers
# or
export LIBSQL_DISABLE_REMOTE_TESTS=1            # skip remote suite
# or
export LIBSQL_DISABLE_TESTCONTAINERS=1          # require LIBSQL_TEST_URL

dotnet test test/Nj.EntityFrameworkCore.LibSql.DriverContractTests -c Release
```

Optional manual compose (same digest as Testcontainers): [`eng/sqld/docker-compose.yml`](../eng/sqld/docker-compose.yml)
(image pinned by digest; see [versions.md](versions.md)).

CI uploads test results (and `sqld` logs on integration failure). Caches cover
NuGet packages only — never secrets or mutable database state.

## Skipping tests

Permanent skips require a waiver in [compatibility.md](compatibility.md).
Temporary investigatory skips must be tracked with an issue and removed quickly.
