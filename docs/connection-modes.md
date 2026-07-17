# Connection modes

Mode is selected **only** through the `Nj.LibSql.Data` connection string (or an
existing `LibSqlConnection`). There are **no** `UseLibSqlLocal` /
`UseLibSqlRemote` / `UseLibSqlReplica` helpers.

```csharp
options.UseLibSql("Data Source=app.db");
// or
options.UseLibSql(existingLibSqlConnection);
```

Property names and aliases: `Data Source` / `DataSource` / `Filename`;
`Auth Token` / `AuthToken` / `Token`. See [architecture.md](architecture.md).

## Local (Preview 1)

```text
Data Source=/path/to/app.db
Data Source=:memory:
```

## Remote — self-hosted `sqld` or Turso (Preview 1)

`http(s)://` and `libsql://` use HTTP Hrana. Explicit `ws://` / `wss://` use
WebSocket. Turso Cloud rejects WebSocket upgrades — use HTTPS / `libsql://`.

```text
Data Source=http://127.0.0.1:8080
Data Source=https://<db>-<org>.turso.io;Auth Token=<token>
```

Tests start a pinned `libsql-server` image via Testcontainers. Override with
`LIBSQL_TEST_URL`, or use [`eng/sqld/docker-compose.yml`](../eng/sqld/docker-compose.yml).
See [testing.md](testing.md).

Creating remote databases and tokens is **out of scope**
([limitations.md](limitations.md)).

## Embedded replica (Preview 2+)

```text
Data Source=local-replica.db;SyncUrl=https://<db>-<org>.turso.io;Auth Token=<token>
```

Optional keys (when enabled later): `SyncInterval`, `ReadYourWrites`, `Offline`.
EF sync will be `DatabaseFacade` extensions that delegate to the driver.

## Redaction

Auth tokens must never appear in default logs or exception messages. Provider
options follow EF sensitive-data logging rules.
