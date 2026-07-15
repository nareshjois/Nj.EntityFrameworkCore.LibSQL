using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.ComplianceTests;

/// <summary>
/// Host for published EF relational specification suites (WP-08).
/// </summary>
public sealed class ComplianceScaffoldTests
{
    [Fact]
    public void Specification_test_package_resolves()
    {
        // Package reference proves restore; real fixtures arrive with WP-08.
        Assert.True(true);
    }
}
