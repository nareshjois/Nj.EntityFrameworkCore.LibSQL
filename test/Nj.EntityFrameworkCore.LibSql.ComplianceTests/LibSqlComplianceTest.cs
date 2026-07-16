using System.Reflection;
using Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;

namespace Nj.EntityFrameworkCore.LibSql.ComplianceTests;

public sealed class LibSqlComplianceTest : RelationalComplianceTestBase
{
    private static readonly Assembly ComplianceAssembly = typeof(LibSqlComplianceTest).Assembly;

    protected override Assembly TargetAssembly { get; } = ComplianceAssembly;

    protected override ICollection<Type> IgnoredTestBases { get; }
        = ComplianceCapabilities.GetIgnoredTestBases(ComplianceAssembly).ToList();

    static LibSqlComplianceTest()
    {
        ComplianceCapabilities.ValidateManifestCoversIgnoredSuites(ComplianceAssembly);
    }
}
