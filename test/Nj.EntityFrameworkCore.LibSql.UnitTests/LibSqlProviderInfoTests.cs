using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.UnitTests;

public sealed class LibSqlProviderInfoTests
{
    [Fact]
    public void PackageId_matches_locked_identity()
    {
        Assert.Equal("Nj.EntityFrameworkCore.LibSql", LibSqlProviderInfo.PackageId);
    }

    [Fact]
    public void GetScaffoldStatus_includes_package_id()
    {
        var status = LibSqlProviderInfo.GetScaffoldStatus();
        Assert.Contains(LibSqlProviderInfo.PackageId, status, StringComparison.Ordinal);
        Assert.Contains("WP-01", status, StringComparison.Ordinal);
    }
}
