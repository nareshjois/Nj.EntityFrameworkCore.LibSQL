# Connection modes

Mode is selected **only** through the Nelknet.LibSQL connection string (or an
existing `Nelknet.LibSQL.Data.LibSQLConnection`). There are **no**
`UseLibSqlLocal` / `UseLibSqlRemote` / `UseLibSqlReplica` helper methods.

Once `UseLibSql` ships, registration looks like EF Sqlite:

```csharp
options.UseLibSql("Data Source=app.db");
// or
options.UseLibSql(existingLibSqlConnection);
```

Connection-string property names and aliases follow
[Nelknet.LibSQL](https://github.com/nelknet/Nelknet.LibSQL)
(`Data Source` / `DataSource` / `Filename`; `Auth Token` / `AuthToken` / `Token`).

## Local (Preview 1)

File-backed or in-memory database on the process machine.

```text
Data Source=/path/to/app.db
Data Source=:memory:
```

## Remote — self-hosted `sqld` or Turso (Preview 1)

HTTP(S) / `libsql://` URL to an already-provisioned database.

```text
# Self-hosted sqld (CI default)
Data Source=http://127.0.0.1:8080

# Turso example
Data Source=https://<db>-<org>.turso.io;Auth Token=<token>
```

Driver remote tests start a pinned `libsql-server` image via Testcontainers.
Override with `LIBSQL_TEST_URL`, or use optional `eng/sqld/docker-compose.yml`.
See [testing.md](testing.md).

Creating the remote database/namespace and minting tokens is **outside** this
provider ([limitations.md](limitations.md)).

## Embedded replica (Preview 2+)

Local file that syncs to a remote primary. Not part of Preview 1.

```text
Data Source=local-replica.db;SyncUrl=https://<db>-<org>.turso.io;Auth Token=<token>
```

Optional Nelknet keys (when enabled in a future preview): `SyncInterval`,
`ReadYourWrites`, `Offline`. The EF sync surface will be `DatabaseFacade`
extension methods that delegate to Nelknet — they will not invent stronger
semantics than the driver.

## Redaction

Auth tokens must never appear in default logs or exception messages. Provider
options follow EF sensitive-data logging rules once implemented.
