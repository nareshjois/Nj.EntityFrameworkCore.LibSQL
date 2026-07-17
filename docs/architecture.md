# Architecture

`Nj.EntityFrameworkCore.LibSql` is an EF Core 10 provider for
[libSQL](https://docs.turso.tech/libsql). ADO.NET lives in-repo:

```text
Application
  → EF Core 10
    → Nj.EntityFrameworkCore.LibSql
      → Nj.LibSql.Data
        → Nj.LibSql.Bindings (local natives)
        → HTTP / WebSocket Hrana (remote)
```

## Packages

| Package | Role |
|---------|------|
| `Nj.EntityFrameworkCore.LibSql` | EF provider (`UseLibSql`, …) |
| `Nj.LibSql.Data` | ADO.NET (`LibSqlConnection`, …) |
| `Nj.LibSql.Bindings` | Native libSQL client libs (3 RIDs) |

The EF project ProjectReferences Data; Data ProjectReferences Bindings.

## Naming

Public types use Microsoft-style `LibSql` compression (same brand as the EF
package): `UseLibSql`, `LibSqlConnection`, namespace `Nj.LibSql.Data`.

Connection modes are selected **only** via the connection string (or an existing
`LibSqlConnection`). There are no `UseLibSqlLocal` / `UseLibSqlRemote` /
`UseLibSqlReplica` helpers. See [connection-modes.md](connection-modes.md).

## Natives

Local file / `:memory:` uses P/Invoke through Bindings. RIDs:
`linux-x64`, `osx-arm64`, `win-x64`.

Official [libsql releases](https://github.com/tursodatabase/libsql/releases)
publish **libsql-server**, not the C client library. Client libs are built and
published from this repo (`.github/workflows/libsql-native.yml` →
`native-libsql-v*` GitHub Releases). Bindings can download those assets
(DuckDB.NET-style) or use committed `runtimes/` for `ManagedOnly` builds.

Pin and bump process: [versions.md](versions.md),
[eng/native/README.md](../eng/native/README.md).

## Remote transports

| URL scheme | Transport |
|------------|-----------|
| `http://` / `https://` / `libsql://` | HTTP Hrana |
| `ws://` / `wss://` | WebSocket Hrana |

Turso Cloud rejects WebSocket upgrades; prefer HTTPS / `libsql://` for Turso.
Large-result streaming over WSS is validated against self-hosted `sqld`
(`ws://`) in CI.

## CI

| Workflow | Scope |
|----------|--------|
| `ci.yml` | Solution build, format, unit/functional/compliance, pack verify |
| `integration.yml` | Remote sqld / compliance slices |
| `libsql-driver.yml` | Path-filtered Data / Bindings / DriverContractTests |
| `libsql-native.yml` | Build/publish native client libs |

Turso driver jobs require secrets `LIBSQL_TEST_URL` and `LIBSQL_TEST_AUTH_TOKEN`
(missing secrets fail — no silent skip).

## Preview scope

- **Preview 1:** local + remote (`sqld` / Turso).
- **Preview 2+:** embedded replica sync via `DatabaseFacade` extensions that
  delegate to the driver (CS keys already accepted as stubs).
