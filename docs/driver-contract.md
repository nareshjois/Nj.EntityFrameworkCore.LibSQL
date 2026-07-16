# Driver contract (WP-02)

Direct ADO.NET contract suite against `Nelknet.LibSQL.Data` **without** EF.
Source: `test/Nj.EntityFrameworkCore.LibSql.DriverContractTests`.

## Native baseline

| Item | Value |
|------|--------|
| Nelknet.LibSQL.Data | Soft-fork `@a312c97` (upstream `0.2.11` + patches) |
| Bundled libSQL (bindings `LIBSQL_VERSION`) | `libsql-server-v0.24.32` (`40c272de…`) |
| Runtime `LibSQLVersion` report | `libSQL Version: 0.2.3` / `SQLite Version: 3.45.1` |

Update this table when Nelknet is bumped.

## Modes

| Mode | How it runs | Status |
|------|-------------|--------|
| Local file | Always (temp `.db` per test) | Covered |
| Remote self-hosted `sqld` | **Testcontainers** starts the pinned image (unless `LIBSQL_TEST_URL` is set) | Covered when Docker is available |
| Embedded replica | Preview 2+ | Deferred (needs SyncUrl/AuthToken harness) |

```bash
# Local + remote (requires Docker for Testcontainers)
dotnet test test/Nj.EntityFrameworkCore.LibSql.DriverContractTests -c Release

# Use an already-running sqld instead of Testcontainers
export LIBSQL_TEST_URL=http://127.0.0.1:8080
dotnet test test/Nj.EntityFrameworkCore.LibSql.DriverContractTests -c Release

# Disable remote entirely
export LIBSQL_DISABLE_REMOTE_TESTS=1
```

Manual compose (optional, same image digest): `./eng/start-sqld.sh`.

## Findings (Nelknet 0.2.10)

### DbProviderFactory

`LibSQLFactory` exists (`Instance`, `ProviderInvariantName`, `RegisterFactory`).
EF Core providers generally construct connections directly via `UseLibSql` /
options and do **not** require `DbProviderFactory` registration. Factory support
is still validated for tooling/interop.

### Unique constraint typing

Duplicate unique values currently throw `LibSQLException` with
`LibSQLErrorCode = 2` (“Internal logic error in SQLite”), **not**
`LibSQLConstraintException`. Parameter values were not observed in the message.
Candidate for an upstream Nelknet issue if typed constraint exceptions are
required for richer EF mapping.

### Syntax / missing-table exceptions

Some statement failures surface as `InvalidOperationException` whose message
embeds the SQLite failure text, rather than a typed `LibSQLException`. EF
exception mapping must accept both shapes.

### Cross-connection transactions

Assigning `command.Transaction` from another connection is **not** reliably
rejected at execute time in 0.2.10. The EF provider must enforce connection
affinity itself (standard EF pattern).

### Savepoints

No first-class `Save`/`Release`/`Rollback` APIs on `LibSQLTransaction`.
SQL `SAVEPOINT` / `ROLLBACK TO` / `RELEASE` works inside a transaction.

### Positional parameters

Parameter names must start with `@`, `:`, `$`, or `?`. Prefer named `@p`
style (EF Core standard).

### Explicit non-coverage (out of Preview 1 local suite)

- Network interruption mid-commit (ambiguous outcome) — needs fault injection.
- Full authentication/TLS matrix against Turso Cloud — optional later.
- Embedded replica sync races — Preview 2+.

## Acceptance (G2)

EF-critical paths covered for local mode: open/close, commands/parameters,
reader metadata, `last_insert_rowid`, transactions (commit/rollback/dispose),
SQL savepoints, and common error classes. Remote smoke + multi-command
transaction covered when `sqld` is available. Remaining gaps above are
documented rather than silently skipped without rationale.
