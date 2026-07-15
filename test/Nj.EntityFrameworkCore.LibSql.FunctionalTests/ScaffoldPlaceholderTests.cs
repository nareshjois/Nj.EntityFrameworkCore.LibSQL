using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

/// <summary>
/// Placeholder suite. EF functional coverage lands after WP-04+; local libSQL
/// smoke lives here once <c>UseLibSql</c> exists.
/// </summary>
public sealed class ScaffoldPlaceholderTests
{
    [Fact]
    public void Provider_assembly_loads()
    {
        Assert.NotNull(typeof(LibSqlProviderInfo).Assembly);
    }
}
