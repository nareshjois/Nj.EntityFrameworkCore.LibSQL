using Nj.EntityFrameworkCore.LibSql.Scaffolding.Internal;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.UnitTests;

public sealed class LibSqlCreateSqlColumnFacetsTests
{
    [Fact]
    public void Reads_autoincrement_and_nocase_collation()
    {
        const string sql =
            """
            CREATE TABLE "ColumnsWithFacets" (
              "Id" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT,
              "DefaultCollation" TEXT,
              "NonDefaultCollation" TEXT COLLATE NOCASE,
              CONSTRAINT "CK" CHECK ("Id" > 0)
            )
            """;

        LibSqlCreateSqlColumnFacets.GetFacets(sql, "Id", out var idCollation, out var idAutoInc);
        Assert.True(idAutoInc);
        Assert.Null(idCollation);

        LibSqlCreateSqlColumnFacets.GetFacets(sql, "DefaultCollation", out var defaultCollation, out var defaultAutoInc);
        Assert.False(defaultAutoInc);
        Assert.Null(defaultCollation);

        LibSqlCreateSqlColumnFacets.GetFacets(sql, "NonDefaultCollation", out var nocase, out var nocaseAutoInc);
        Assert.False(nocaseAutoInc);
        Assert.Equal("NOCASE", nocase);
    }

    [Fact]
    public void Reads_quoted_collation_name()
    {
        const string sql =
            """
            CREATE TABLE T (
              Name TEXT COLLATE "NOCASE"
            )
            """;

        LibSqlCreateSqlColumnFacets.GetFacets(sql, "Name", out var collation, out var autoIncrement);
        Assert.False(autoIncrement);
        Assert.Equal("NOCASE", collation);
    }

    [Fact]
    public void Ignores_views()
    {
        const string sql = """CREATE VIEW V AS SELECT 1 AS Id""";
        LibSqlCreateSqlColumnFacets.GetFacets(sql, "Id", out var collation, out var autoIncrement);
        Assert.False(autoIncrement);
        Assert.Null(collation);
    }
}
