# Migrations

This provider will support the normal EF Core migrations workflow
(`Add-Migration` / `dotnet ef migrations add`, `Database.Migrate`, history table,
and migration locking) once the migrations work package is complete.

Until then, design-time samples under `samples/MigrationsSample` are stubs.

## Locked create / delete policy

These rules will not change without a documented compatibility impact:

| Mode | `EnsureCreated` | `EnsureDeleted` |
|------|-----------------|-----------------|
| Local file | May create the database file and apply model schema (EF SQLite–like) | May delete the database file |
| Remote (`sqld` / Turso) | Creates schema **inside** an already-addressable database only — never creates cloud databases or namespaces | Always throws `NotSupportedException` with a clear message |
| Embedded replica | Schema inside the replica database only | Always throws `NotSupportedException` |

## Migration history

History schema is expected to remain compatible with the EF SQLite provider
unless a Nelknet/libSQL incompatibility forces a documented delta. Any
divergence must be recorded in [compatibility.md](compatibility.md).

## Design-time tooling

Design-time services ship in the same NuGet package
(`Nj.EntityFrameworkCore.LibSql`). Install `dotnet-ef` matching the EF Core
patch in [versions.md](versions.md).
