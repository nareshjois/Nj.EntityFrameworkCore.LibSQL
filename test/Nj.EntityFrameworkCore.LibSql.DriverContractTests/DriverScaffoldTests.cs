using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.DriverContractTests;

/// <summary>
/// Direct ADO.NET contract suite against Nelknet (WP-02). No EF dependency.
/// </summary>
public sealed class DriverScaffoldTests
{
    [Fact]
    public void Nelknet_connection_type_is_available()
    {
        var type = Type.GetType("Nelknet.LibSQL.Data.LibSQLConnection, Nelknet.LibSQL.Data");
        Assert.NotNull(type);
    }
}
