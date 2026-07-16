# MigrationsSample (G9)

End-to-end design-time sample for `Nj.EntityFrameworkCore.LibSql`: migrate a
Blog/Post model, or scaffold from an existing database, with local file or
remote `sqld`.

Requires `dotnet-ef` **10.0.10** (see [docs/versions.md](../../docs/versions.md)):

```bash
dotnet tool install --global dotnet-ef --version 10.0.10
# or: dotnet tool update --global dotnet-ef --version 10.0.10
```

## Connection

| Mode | How |
|------|-----|
| Local (default) | `LIBSQL_CONNECTION` unset → `Data Source=…/migrations-sample.db` under the build output |
| Explicit local | `export LIBSQL_CONNECTION='Data Source=/tmp/g9.db'` |
| Remote `sqld` | `export LIBSQL_CONNECTION='Data Source=http://127.0.0.1:8080'` (or your Turso/HTTP URL) |

`SampleDbContextFactory` (`IDesignTimeDbContextFactory`) reads `LIBSQL_CONNECTION`.

## Path A — migrations from the hand-written model

From the repo root:

```bash
export LIBSQL_CONNECTION="Data Source=/tmp/nj-migrations-sample.db"
rm -f /tmp/nj-migrations-sample.db

dotnet ef migrations add InitialCreate \
  --project samples/MigrationsSample/MigrationsSample.csproj \
  --output-dir Migrations

dotnet ef database update \
  --project samples/MigrationsSample/MigrationsSample.csproj

dotnet run --project samples/MigrationsSample/MigrationsSample.csproj
```

Remote: point `LIBSQL_CONNECTION` at `sqld`, then the same `database update` /
`dotnet run` steps (remote `EnsureDeleted` is not supported—use a fresh DB).

## Path B — scaffold from an existing database

Apply [`seed/schema.sql`](seed/schema.sql) to an empty DB (or use
`eng/verify-migrations-sample.sh`), then:

```bash
export LIBSQL_CONNECTION="Data Source=/tmp/nj-scaffold-source.db"

dotnet ef dbcontext scaffold "$LIBSQL_CONNECTION" Nj.EntityFrameworkCore.LibSql \
  --project samples/MigrationsSample/MigrationsSample.csproj \
  --output-dir Scaffolded \
  --context ScaffoldedContext \
  --force
```

Build the project after scaffolding to confirm generated entities compile.
Generated output is gitignored under `Scaffolded/` when produced by the verify
script.

## Migration scripts

```bash
# Supported (non-idempotent):
dotnet ef migrations script --project samples/MigrationsSample/MigrationsSample.csproj

# Not supported (SQLite/libSQL parity — throws):
dotnet ef migrations script --idempotent --project samples/MigrationsSample/MigrationsSample.csproj
```

See [docs/migrations.md](../../docs/migrations.md).

## Automated verify

```bash
./eng/verify-migrations-sample.sh
# Remote (optional): LIBSQL_CONNECTION='Data Source=http://…' ./eng/verify-migrations-sample.sh --remote
```
