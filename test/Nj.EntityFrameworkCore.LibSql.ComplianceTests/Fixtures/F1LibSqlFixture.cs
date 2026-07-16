using Microsoft.EntityFrameworkCore.TestUtilities;
using Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;

namespace Nj.EntityFrameworkCore.LibSql.ComplianceTests.Fixtures;

public class F1LibSqlFixture : F1LibSqlFixtureBase<byte[]>;

public class F1ULongLibSqlFixture : F1LibSqlFixtureBase<ulong?>
{
    protected override string StoreName
        => "F1ULongLibSqlTest";
}

public abstract class F1LibSqlFixtureBase<TRowVersion> : F1RelationalFixture<TRowVersion>
{
    protected override ITestStoreFactory TestStoreFactory
        => LibSqlTestStoreFactory.Instance;

    public override TestHelpers TestHelpers
        => LibSqlTestHelpers.Instance;
}
