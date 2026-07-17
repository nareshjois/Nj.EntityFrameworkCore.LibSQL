# Testing

## Test projects

| Project | Role |
|---------|------|
| `Nj.EntityFrameworkCore.LibSql.UnitTests` | Fast provider unit tests |
| `Nj.EntityFrameworkCore.LibSql.FunctionalTests` | EF + local/remote integration |
| `Nj.EntityFrameworkCore.LibSql.ComplianceTests` | Published EF relational specification suites |
| `Nj.LibSql.DriverContractTests` | ADO.NET contract for `Nj.LibSql.Data` (no EF) |
| `Nj.EntityFrameworkCore.LibSql.PackageTests` | Pack + clean NuGet install verification |
| `TestUtilities` | Shared helpers (connection strings, env) |

Primary suites use **xUnit v3**. `ComplianceTests` stays on xUnit v2 while EF
`Specification.Tests` packages depend on `xunit.core` 2.9.x.

## Commands

```bash
# Format (imported EF baseline excluded)
dotnet format Nj.EntityFrameworkCore.LibSql.slnx --verify-no-changes \
  --exclude ./src/Nj.EntityFrameworkCore.LibSql/**

dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release

dotnet test test/Nj.EntityFrameworkCore.LibSql.UnitTests -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release

# ConnectionModes (local always; sqld/replica need Docker; Turso needs secrets)
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release \
  --filter "FullyQualifiedName~ConnectionModes"

# Local compliance gate (excludes remote BuiltInDataTypes)
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release \
  --filter "FullyQualifiedName!~BuiltInDataTypesRemoteLibSqlTest"

# Remote compliance slice (C-016)
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release \
  --filter "FullyQualifiedName~Remote"

./eng/generate-compliance-report.sh
./eng/pack.sh
./eng/verify-package.sh
```

On Windows, use the corresponding `eng/*.ps1` scripts.

Driver / Data / Bindings local CI mirror:

```bash
./eng/verify-driver-ci-local.sh
```

## Driver contract (`Nj.LibSql.Data`)

```bash
# Local + remote (Docker for Testcontainers)
dotnet test test/Nj.LibSql.DriverContractTests -c Release

export LIBSQL_TEST_URL=http://127.0.0.1:8080   # skip Testcontainers
export LIBSQL_DISABLE_REMOTE_TESTS=1            # local only
export LIBSQL_REQUIRE_REMOTE=1                  # fail instead of skip (CI)

# Turso (HTTP; secrets required in CI turso job)
export LIBSQL_TEST_URL=libsql://…
export LIBSQL_TEST_AUTH_TOKEN=…
export LIBSQL_REQUIRE_TURSO=1
export LIBSQL_DISABLE_TESTCONTAINERS=1
dotnet test test/Nj.LibSql.DriverContractTests -c Release --filter "FullyQualifiedName~Turso"

# Embedded replica Sync (always uses Testcontainers sqld; ignores Turso URL)
dotnet test test/Nj.LibSql.DriverContractTests -c Release --filter "FullyQualifiedName~EmbeddedReplica"
```

Path-filtered CI: `.github/workflows/libsql-driver.yml` (`local`, `remote-sqld` incl. EmbeddedReplica, `turso`).
Native rebuild: `.github/workflows/libsql-native.yml` +
[eng/native/README.md](../eng/native/README.md).

Secrets: `LIBSQL_TEST_URL`, `LIBSQL_TEST_AUTH_TOKEN` — missing secrets fail the
Turso job (no silent skip). Embedded-replica Sync against Turso is **C-019**.

Optional manual compose: [`eng/sqld/docker-compose.yml`](../eng/sqld/docker-compose.yml)
(image digest in [versions.md](versions.md)).

## Skipping tests

Permanent skips need a waiver in [compatibility.md](compatibility.md).
Temporary skips need an issue and a quick follow-up.
