# Migrations

This provider supports the EF Core migrations **runtime** workflow
(`Database.Migrate`, history table, migration locking) as validated by the
WP-08 FunctionalTests matrix. Design-time (`dotnet ef migrations add` /
`database update`) lands with WP-09; samples under `samples/MigrationsSample`
remain stubs until then.

## Preview status

| Surface | Status |
|---------|--------|
| `EnsureCreated` (schema in addressable DB) | Working (local + remote) |
| `EnsureDeleted` local | Working (may delete file) |
| `EnsureDeleted` remote / replica | Throws `NotSupportedException` |
| `Database.Migrate` up / down | Working (FunctionalTests matrix) |
| Migration lock table acquire/release | Working (Nelknet-safe split commands) |
| Idempotent SQL script generation | Not supported (SQLite/libSQL limitation; throws) |
| `dotnet ef` design-time | WP-09 |

See [wp-08-handoff.md](wp-08-handoff.md).

## Locked create / delete policy

These rules will not change without a documented compatibility impact:

| Mode | `EnsureCreated` | `EnsureDeleted` |
|------|-----------------|-----------------|
| Local file | May create the database file and apply model schema (EF SQLite–like) | May delete the database file |
| Remote (`sqld` / Turso) | Creates schema **inside** an already-addressable database only — never creates cloud databases or namespaces | Always throws `NotSupportedException` with a clear message |
| Embedded replica | Schema inside the replica database only | Always throws `NotSupportedException` |

## Migration history

History schema remains compatible with the EF SQLite provider
(`__EFMigrationsHistory`, `__EFMigrationsLock`) unless a Nelknet/libSQL
incompatibility forces a documented delta. Any divergence must be recorded in
[compatibility.md](compatibility.md).

## Design-time tooling

Design-time services ship in the same NuGet package
(`Nj.EntityFrameworkCore.LibSql`). Install `dotnet-ef` matching the EF Core
patch in [versions.md](versions.md).
