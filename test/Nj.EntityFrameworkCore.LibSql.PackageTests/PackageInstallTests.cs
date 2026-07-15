using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.PackageTests;

public sealed class PackageInstallTests
{
    [Fact]
    public void Scaffold_status_is_reachable_from_installed_surface()
    {
        var status = LibSqlProviderInfo.GetScaffoldStatus();
        Assert.StartsWith(LibSqlProviderInfo.PackageId, status, StringComparison.Ordinal);
    }
}
