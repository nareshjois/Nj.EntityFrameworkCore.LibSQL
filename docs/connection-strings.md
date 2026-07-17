# Connection strings

Source of truth: `Nj.LibSql.Data.LibSqlConnectionStringBuilder`
([src/Nj.LibSql.Data/LibSqlConnectionStringBuilder.cs](../src/Nj.LibSql.Data/LibSqlConnectionStringBuilder.cs)).
Mode is inferred from the keys you set — there are no separate `UseLibSqlLocal` /
`UseLibSqlRemote` helpers. See [connection-modes.md](connection-modes.md).

## Keys

| Canonical key | Aliases | Purpose |
|---------------|---------|---------|
| `Data Source` | `DataSource`, `Database`, `DB`, `Uri`, `Url`, `Filename`, `File Name` | File path, `:memory:`, or remote URL (`http(s)://`, `libsql://`, `ws(s)://`) |
| `Auth Token` | `AuthToken`, `Token` | Remote / Turso auth; also default sync auth for replicas |
| `Encryption Key` | `EncryptionKey`, `Key` | Local encrypted DB key |
| `Sync URL` | `SyncURL`, `SyncUrl` | Embedded-replica primary URL (sets replica mode) |
| `Sync Auth Token` | `SyncAuthToken`, `SyncToken` | Optional sync-specific token (else `Auth Token`) |
| `Sync Interval` | `SyncInterval` | Auto-sync interval in **seconds** (`0` = manual `Sync` only) |
| `Read Your Writes` | `ReadYourWrites` | Default `true`; local writes visible before next sync |
| `Offline` | — | Appends `?offline` to sync URL until first `Sync` |
| `Tls` | — | Default `true`. When `false`, maps `libsql://` → `http://` (local sqld); when `true`, → `https://` |

Also accepted and **ignored** (Microsoft.Data.Sqlite soft migration): `Mode`, `Cache`,
`Foreign Keys`, `Recursive Triggers`, `Pooling`, `Vfs`, `Default Timeout` /
`Command Timeout`.

Named parameters accept `@name`, `:name`, and `$name` interchangeably.

Use `LibSqlConnectionStringBuilder.Redact` before logging connection strings.

## Examples

```text
Data Source=/tmp/app.db
Filename=/tmp/app.db
Data Source=:memory:
Data Source=http://127.0.0.1:8080
Data Source=libsql://127.0.0.1:8080;Tls=False
Data Source=libsql://my-db-org.turso.io;Auth Token=…
Data Source=/tmp/replica.db;Sync URL=http://127.0.0.1:8080;Read Your Writes=False
Data Source=/tmp/replica.db;Sync URL=https://…;Auth Token=…;Sync Interval=30
```

## Related

- [connection-modes.md](connection-modes.md) — local / remote / replica behavior
- [turso-dotnet-comparison.md](turso-dotnet-comparison.md) — Nj vs Turso.Data matrix
- [transactions.md](transactions.md) — cancel, retries, Sync
- [observability.md](observability.md) — redaction in diagnostics
