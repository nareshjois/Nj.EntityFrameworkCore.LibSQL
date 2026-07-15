# Provider service map

Subsystem inventory of the imported / renamed EF SQLite provider and the
expected customization marks before functional LibSQL work (WP-04+).

Legend:

| Mark | Meaning |
|------|---------|
| `SQLite-compatible` | Keep behavior; only mechanical naming so far |
| `Nelknet connection adapt` | Must stop using `Microsoft.Data.Sqlite*` |
| `libSQL semantic` | May need capability/mode-aware behavior |
| `deferred` | Preview 2+ or later work |
| `unsupported` | Explicit non-goal; do not emulate |

## Vertical slice (Preview 1 target)

Minimum path for WP-04 exit: `UseLibSql` → model creation → `SELECT 1` → CRUD →
migrations on **local** and **remote** modes.

## Subsystems

| Subsystem | Key types (post-rename) | Mark | Notes |
|-----------|-------------------------|------|-------|
| DI registration | `AddEntityFrameworkLibSql`, `DatabaseProvider<LibSqlOptionsExtension>` | `SQLite-compatible` | WP-04 verifies hashing / pooling |
| Options / `UseLibSql` | `LibSqlDbContextOptionsBuilderExtensions`, `LibSqlOptionsExtension`, `LibSqlDbContextOptionsBuilder` | `Nelknet connection adapt` | Accept connection string or `LibSQLConnection`; reject incompatible types |
| Conventions | `LibSqlConventionSetBuilder` and related | `SQLite-compatible` | |
| Type mapping | `LibSqlTypeMappingSource`, JSON readers | `SQLite-compatible` | Round-trip validate vs Nelknet in WP-05 |
| SQL generation helper | `LibSqlSqlGenerationHelper` | `SQLite-compatible` | |
| Query translation | `LibSqlQuerySqlGenerator`, translators, nullability | `SQLite-compatible` / server-version → `libSQL semantic` | Replace `new SqliteConnection().ServerVersion` checks with capability service |
| Updates | `LibSqlUpdateSqlGenerator`, modification command factories | `SQLite-compatible` / RETURNING version check → `libSQL semantic` | Same as query version gates |
| Transactions | inherits Relational | `Nelknet connection adapt` | Enforce affinity per WP-02 findings |
| Migrations | `LibSqlMigrationsSqlGenerator`, history repository | `SQLite-compatible` | |
| Database creation | `LibSqlDatabaseCreator` | `Nelknet connection adapt` / `libSQL semantic` | Remote `EnsureDeleted` unsupported — see [capabilities.md](capabilities.md) |
| Relational connection | `LibSqlRelationalConnection`, `ILibSqlRelationalConnection` | `Nelknet connection adapt` | First-touch |
| Scaffolding / model factory | `LibSqlDatabaseModelFactory`, `LibSqlCodeGenerator` | `Nelknet connection adapt` | Uses `SqliteConnection` / P/Invoke today |
| Design-time | `LibSqlDesignTimeServices` | `Nelknet connection adapt` | |
| Diagnostics | `LibSqlEventId`, logging definitions | `SQLite-compatible` | |
| SpatiaLite | `SpatialiteLoader` | `unsupported` | Loadable extensions out of scope ([limitations.md](limitations.md)) |
| Embedded replica | — | `deferred` | Preview 2+ |
| Shared utilities | `src/.../Shared/*` | `SQLite-compatible` | Copied from `dotnet/efcore` Shared (internal helpers) |

## Expected first code-touch list (WP-04)

Functional review fails if edits outside this list lack a test-backed design note:

1. `LibSqlRelationalConnection` / `ILibSqlRelationalConnection`
2. `LibSqlDatabaseCreator`
3. `LibSqlDatabaseModelFactory` (and related scaffolding)
4. `SpatialiteLoader` (remove or hard-fail NotSupported)
5. Server-version probes in `LibSqlUpdateSqlGenerator` and `LibSqlQueryableMethodTranslatingExpressionVisitor`
6. Options extension connection-string / existing-connection paths (`UseLibSql`)
7. Dependency package: drop `Microsoft.Data.Sqlite.Core`

## Public API rename summary

| Upstream | This provider |
|----------|---------------|
| `UseSqlite` | `UseLibSql` |
| `AddEntityFrameworkSqlite` | `AddEntityFrameworkLibSql` |
| `SqliteDbContextOptionsBuilder` | `LibSqlDbContextOptionsBuilder` |
| `Sqlite*` provider types | `LibSql*` |

SpatiaLite helper types remain named `SpatialiteLoader` but are **unsupported**.
