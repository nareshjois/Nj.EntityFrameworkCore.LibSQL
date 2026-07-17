# Nj.EntityFrameworkCore.LibSql

Community [EF Core](https://learn.microsoft.com/ef/core/) 10 provider for
[libSQL](https://docs.turso.tech/libsql), backed by in-repo
[`Nj.LibSql.Data`](src/Nj.LibSql.Data) ([architecture](docs/architecture.md)).

> **Status:** [`10.0.0-preview.1`](https://www.nuget.org/packages/Nj.EntityFrameworkCore.LibSql/10.0.0-preview.1)
> (prerelease on NuGet.org). APIs may change. Do **not** use in production until a
> stable `10.0.x` release. See [releasing](docs/releasing.md).

```bash
dotnet add package Nj.EntityFrameworkCore.LibSql --prerelease
```

## Package identity

| Item | Value |
|------|--------|
| NuGet package | `Nj.EntityFrameworkCore.LibSql` (also `Nj.LibSql.Data`, `Nj.LibSql.Bindings`) |
| Namespace / assembly | `Nj.EntityFrameworkCore.LibSql` |
| Version | `10.0.0-preview.1` |
| License | MIT |
| EF Core | `10.0.10` |
| ADO.NET | `Nj.LibSql.Data` + `Nj.LibSql.Bindings` ([versions](docs/versions.md)) |

Public API follows Microsoft’s Sqlite naming pattern (`UseLibSql`,
`AddEntityFrameworkLibSql`, …). Connection modes are selected **only** via the
connection string (or an existing `LibSqlConnection`) — no mode-specific `Use*`
helpers. Reference: [connection-strings.md](docs/connection-strings.md),
[connection-modes.md](docs/connection-modes.md).

## Preview modes

| Mode | Preview | Notes |
|------|---------|--------|
| Local libSQL file | Yes | File create/delete like EF SQLite |
| Remote self-hosted `sqld` / Turso | Yes | Database must already exist; Turso via HTTPS/`libsql://` (Cloud rejects WebSocket) |
| Embedded replica | Yes (vs `sqld`) | `Database.Sync` / `LibSqlConnection.Sync`; Turso Cloud Sync hangs — **C-019** |

## Quick start — local

```bash
dotnet run --project samples/LocalSample
```

Or in code:

```csharp
optionsBuilder.UseLibSql("Data Source=/tmp/app.db");
```

## Quick start — remote (`sqld` or Turso)

```bash
./eng/start-sqld.sh && ./eng/wait-for-sqld.sh   # self-hosted
dotnet run --project samples/RemoteSample
# or: LIBSQL_SAMPLE_CONNECTION='Data Source=libsql://…;Auth Token=…' dotnet run --project samples/RemoteSample
```

```csharp
optionsBuilder.UseLibSql("Data Source=http://127.0.0.1:8080");
// Turso: Data Source=libsql://<db>-<org>.turso.io;Auth Token=<token>
```

## Quick start — embedded replica

Requires a reachable primary (self-hosted `sqld` recommended; Turso Sync is
**C-019**):

```bash
./eng/start-sqld.sh && ./eng/wait-for-sqld.sh
dotnet run --project samples/EmbeddedReplicaSample
```

```csharp
optionsBuilder.UseLibSql(
    "Data Source=/tmp/replica.db;Sync URL=http://127.0.0.1:8080;Read Your Writes=False");
// then: await context.Database.SyncAsync();
```

## DI, pooling, existing connection

```bash
dotnet run --project samples/DiPoolingSample
```

Covers `AddDbContextPool`, `AddDbContextFactory`, and `UseLibSql(LibSqlConnection)`.

## Migrations / design-time

See [samples/MigrationsSample](samples/MigrationsSample/README.md) (`dotnet-ef`
10.0.10, `IDesignTimeDbContextFactory`, migrate + scaffold). Overview:
[docs/migrations.md](docs/migrations.md).

## Contributor build (no private secrets)

```bash
dotnet restore Nj.EntityFrameworkCore.LibSql.slnx
dotnet build Nj.EntityFrameworkCore.LibSql.slnx -c Release
dotnet test test/Nj.EntityFrameworkCore.LibSql.UnitTests -c Release
dotnet run --project samples/LocalSample
./eng/start-sqld.sh && ./eng/wait-for-sqld.sh
dotnet run --project samples/RemoteSample
```

Full guide: [CONTRIBUTING.md](CONTRIBUTING.md).

## Documentation

- [Docs index](docs/README.md)
- [Architecture](docs/architecture.md)
- [Connection modes](docs/connection-modes.md) · [Connection strings](docs/connection-strings.md)
- [Migrate from EF SQLite](docs/migrate-from-ef-sqlite.md)
- [Transactions](docs/transactions.md) · [Deployment](docs/deployment.md)
- [Compatibility](docs/compatibility.md) · [Limitations](docs/limitations.md)
- [Releasing](docs/releasing.md) · [Observability](docs/observability.md)
- [Contributing](CONTRIBUTING.md) · [Security](SECURITY.md)

## Repository

- **GitHub:** [nareshjois/Nj.EntityFrameworkCore.LibSQL](https://github.com/nareshjois/Nj.EntityFrameworkCore.LibSQL)
- **Attribution:** [NOTICE](NOTICE)
- **Changelog:** [CHANGELOG.md](CHANGELOG.md)

## License

MIT — see [LICENSE](LICENSE). Imported EF Core SQLite provider source retains
Microsoft’s MIT copyright headers; community modifications are attributed in
[NOTICE](NOTICE).
