namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Scaffolding;

/// <summary>
/// Shared DDL for the scaffolding FunctionalTests matrix.
/// </summary>
internal static class ScaffoldingSampleSchema
{
    public static readonly string[] Statements =
    [
        "DROP VIEW IF EXISTS \"ParentNames\"",
        "DROP TABLE IF EXISTS \"Children\"",
        "DROP TABLE IF EXISTS \"Parents\"",
        "DROP TABLE IF EXISTS \"__EFMigrationsHistory\"",
        """
        CREATE TABLE "Parents" (
          "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
          "Name" TEXT NOT NULL DEFAULT 'anon',
          "Label" TEXT COLLATE NOCASE
        )
        """,
        """
        CREATE TABLE "Children" (
          "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
          "ParentId" INTEGER NOT NULL,
          "Code" TEXT NOT NULL,
          FOREIGN KEY ("ParentId") REFERENCES "Parents" ("Id")
        )
        """,
        "CREATE UNIQUE INDEX \"IX_Children_Code\" ON \"Children\" (\"Code\")",
        """
        CREATE VIEW "ParentNames" AS
        SELECT "Id", "Name" FROM "Parents"
        """,
        """
        CREATE TABLE "__EFMigrationsHistory" (
          "MigrationId" TEXT NOT NULL CONSTRAINT "PK___EFMigrationsHistory" PRIMARY KEY,
          "ProductVersion" TEXT NOT NULL
        )
        """
    ];
}
