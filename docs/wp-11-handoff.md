# WP-11 handoff — Connection modes + embedded replica sync (G10)

## Summary

Closed acceptance gate **G10** for Preview 1 connection modes on
`Nj.LibSql.Data`:

| Mode | Suite | CI |
|------|-------|-----|
| Local | `FunctionalTests/ConnectionModes/Local*` | `ci.yml` |
| Remote sqld | DriverContract Remote + ConnectionModes sqld + fault injection | `integration.yml` `remote-sqld` |
| Remote Turso HTTP | ConnectionModes Turso (required secrets) | `integration.yml` `turso` |
| Embedded replica | DriverContract `EmbeddedReplica*` + EF `Database.Sync` | `integration.yml` / `libsql-driver.yml` remote-sqld |

## Public API

- `LibSqlConnection.Sync` / `SyncAsync` → `LibSqlSyncResult`
- `DatabaseFacade.Sync` / `SyncAsync` (EF) — delegates to open `LibSqlConnection`
- CS keys: `Sync URL`, `Sync Auth Token`, `Sync Interval` (**seconds**),
  `Read Your Writes`, `Offline` (`?offline` on sync URL for pinned natives)

## Known limit — C-019

Turso Cloud embedded-replica **Sync hangs** with the pinned native client
(`libsql-server-v0.24.32`). Replica Sync is **validated against self-hosted
sqld**. Turso remains the required HTTP remote gate.

## Docs / samples

- [connection-modes.md](connection-modes.md), [compatibility.md](compatibility.md)
  (C-018, C-019), [architecture.md](architecture.md), [limitations.md](limitations.md),
  [testing.md](testing.md)
- `samples/RemoteSample`, `samples/EmbeddedReplicaSample`

## Regression gates kept green

Unit + DriverContract local + Functional (non-Turso) + Compliance local gate.
