# Testing

## Test projects

| Project | Role |
|---------|------|
| `Nj.EntityFrameworkCore.LibSql.UnitTests` | Fast provider unit tests |
| `Nj.EntityFrameworkCore.LibSql.FunctionalTests` | EF + local/remote integration |
| `Nj.EntityFrameworkCore.LibSql.ComplianceTests` | Published EF relational specification suites |
| `Nj.EntityFrameworkCore.LibSql.DriverContractTests` | Soft-fork (Nelknet) ADO.NET contract — EF default until cutover |
| `Nj.LibSql.DriverContractTests` | Clean-driver (`Nj.LibSql.Data`) contract mirror ([ADR-0002](adr/0002-nj-libsql-data.md)); path-filtered CI via `.github/workflows/libsql-driver.yml` |
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

# Functional + compliance tests
dotnet test test/Nj.EntityFrameworkCore.LibSql.FunctionalTests -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release

# Local compliance gate (excludes remote BuiltInDataTypes; CI parity)
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release \
  --filter "FullyQualifiedName!~BuiltInDataTypesRemoteLibSqlTest"

# Remote compliance slice (integration; non-blocking tracked under C-016)
dotnet test test/Nj.EntityFrameworkCore.LibSql.ComplianceTests -c Release \
  --filter "FullyQualifiedName~Remote"

# Compliance report artifact
./eng/generate-compliance-report.sh

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

# Soft-fork driver contract (EF default until cutover)
dotnet test test/Nj.EntityFrameworkCore.LibSql.DriverContractTests -c Release

# Clean driver (Nj.LibSql.Data) — Phase 2: local + Testcontainers sqld; Turso when secrets set
dotnet test test/Nj.LibSql.DriverContractTests -c Release

# Force remote failure instead of skip (CI remote-sqld job)
export LIBSQL_REQUIRE_REMOTE=1

# Turso remote (HTTP; CI turso job; secrets required)
# WSS large-result gate: Testcontainers sqld via ws:// (remote-sqld job)
export LIBSQL_TEST_URL=libsql://eftest-….turso.io
export LIBSQL_TEST_AUTH_TOKEN=…
export LIBSQL_REQUIRE_TURSO=1
export LIBSQL_DISABLE_TESTCONTAINERS=1
dotnet test test/Nj.LibSql.DriverContractTests -c Release --filter "FullyQualifiedName~Turso"
```

Path-filtered CI: `.github/workflows/libsql-driver.yml` (jobs: `local`, `remote-sqld`, `turso`).
Native rebuild/publish: `.github/workflows/libsql-native.yml` + [eng/native/README.md](../eng/native/README.md).
Turso secrets: `LIBSQL_TEST_URL`, `LIBSQL_TEST_AUTH_TOKEN` (missing secrets fail the `turso` job — no silent skip).

Optional manual compose (same digest as Testcontainers): [`eng/sqld/docker-compose.yml`](../eng/sqld/docker-compose.yml)
(image pinned by digest; see [versions.md](versions.md)).

CI uploads test results (and `sqld` logs on integration failure). Caches cover
NuGet packages only — never secrets or mutable database state.

## Skipping tests

Permanent skips require a waiver in [compatibility.md](compatibility.md).
Temporary investigatory skips must be tracked with an issue and removed quickly.
