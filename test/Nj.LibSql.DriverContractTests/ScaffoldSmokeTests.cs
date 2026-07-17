using Nj.EntityFrameworkCore.LibSql.TestUtilities;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.LibSql.DriverContractTests;

/// <summary>Unskipped Phase 0 smoke: types construct without opening a database.</summary>
public sealed class ScaffoldSmokeTests
{
    [Fact]
    public void Public_types_can_be_constructed()
    {
        var connection = new LibSqlConnection(TestEnvironment.InMemoryConnectionString);
        Assert.Equal(TestEnvironment.InMemoryConnectionString, connection.ConnectionString);

        var command = connection.CreateCommand();
        Assert.IsType<LibSqlCommand>(command);
        command.CommandText = "SELECT 1";
        command.CommandTimeout = 5;
        Assert.Equal(5, command.CommandTimeout);

        Assert.NotNull(LibSqlFactory.Instance);
        Assert.Equal("Nj.LibSql.Data", LibSqlFactory.ProviderInvariantName);
        Assert.IsType<LibSqlConnection>(LibSqlFactory.Instance.CreateConnection());
        Assert.IsType<LibSqlCommand>(LibSqlFactory.Instance.CreateCommand());
        Assert.IsType<LibSqlParameter>(LibSqlFactory.Instance.CreateParameter());
        Assert.IsType<LibSqlConnectionStringBuilder>(
            LibSqlFactory.Instance.CreateConnectionStringBuilder());

        var builder = new LibSqlConnectionStringBuilder("Data Source=:memory:;Auth Token=x");
        Assert.Equal(":memory:", builder.DataSource);
        Assert.Equal("x", builder.AuthToken);

        var parameter = new LibSqlParameter("p0", 1);
        Assert.Equal("@p0", parameter.ParameterName);

        Assert.False(string.IsNullOrWhiteSpace(LibSqlVersion.GetVersionInfo()));
    }

    [Fact]
    public void RegisterFactory_does_not_throw()
    {
        LibSqlFactory.RegisterFactory();
        LibSqlFactory.UnregisterFactory();
    }
}
