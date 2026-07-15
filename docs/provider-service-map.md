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

## Vertical slice (Preview 1 / G4)

`UseLibSql` → model creation → `SELECT 1` on **local** and **remote**, plus DI /
factory / pooled factory smoke. CRUD + migrations deepen in later WPs.

## Subsystems

| Subsystem | Key types (post-rename) | Mark | Notes |
|-----------|-------------------------|------|-------|
| DI registration | `AddEntityFrameworkLibSql`, `DatabaseProvider<LibSqlOptionsExtension>` | `SQLite-compatible` | Verified DI/factory/pooled smoke in FunctionalTests |
| Options / `UseLibSql` | `LibSqlDbContextOptionsBuilderExtensions`, `LibSqlOptionsExtension`, `LibSqlDbContextOptionsBuilder` | `Nelknet connection adapt` | Requires `LibSQLConnection`; SpatiaLite throws |
| Conventions | `LibSqlConventionSetBuilder` and related | `SQLite-compatible` | |
| Type mapping | `LibSqlTypeMappingSource`, JSON readers | `SQLite-compatible` | Round-trip validate vs Nelknet in WP-05 |
| SQL generation helper | `LibSqlSqlGenerationHelper` | `SQLite-compatible` | |
| Query translation | `LibSqlQuerySqlGenerator`, translators, nullability | `SQLite-compatible` / `libSQL semantic` | Version gates via `LibSqlDatabaseCapabilities` |
| Updates | `LibSqlUpdateSqlGenerator`, modification command factories | `SQLite-compatible` / `libSQL semantic` | RETURNING via capabilities |
| Transactions | inherits Relational | `Nelknet connection adapt` | Enforce affinity per WP-02 findings |
| Migrations | `LibSqlMigrationsSqlGenerator`, history repository | `SQLite-compatible` | |
| Database creation | `LibSqlDatabaseCreator` | `Nelknet connection adapt` / `libSQL semantic` | Remote delete unsupported |
| Relational connection | `LibSqlRelationalConnection`, `ILibSqlRelationalConnection` | `Nelknet connection adapt` | Done (WP-04) |
| Scaffolding / model factory | `LibSqlDatabaseModelFactory`, `LibSqlCodeGenerator` | `Nelknet connection adapt` | Compiles on LibSQLConnection; native metadata deferred |
| Design-time | `LibSqlDesignTimeServices` | `Nelknet connection adapt` | |
| Diagnostics | `LibSqlEventId`, logging definitions | `SQLite-compatible` | |
| SpatiaLite | `SpatialiteLoader`, `UseSpatialite` | `unsupported` | Hard-fail; Nelknet has no load_extension |
| Query / updates version gates | `LibSqlDatabaseCapabilities` | `libSQL semantic` | Static 3.45.1 capability stub (WP-04) |
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
