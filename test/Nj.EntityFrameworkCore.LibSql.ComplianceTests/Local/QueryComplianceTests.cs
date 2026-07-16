using Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore.Query;

#nullable disable

public class OperatorsQueryLibSqlTest(NonSharedFixture fixture)
    : OperatorsQueryTestBase(fixture)
{
    protected override ITestStoreFactory TestStoreFactory
        => LibSqlTestStoreFactory.Instance;
}

public class InheritanceQueryLibSqlTest(TPHInheritanceQueryLibSqlFixture fixture, ITestOutputHelper testOutputHelper)
    : TPHInheritanceQueryTestBase<TPHInheritanceQueryLibSqlFixture>(fixture, testOutputHelper)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());
}

public class TPHInheritanceQueryLibSqlFixture : TPHInheritanceQueryFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => LibSqlTestStoreFactory.Instance;
}

public class TPTInheritanceQueryLibSqlTest(TPTInheritanceQueryLibSqlFixture fixture, ITestOutputHelper testOutputHelper)
    : TPTInheritanceQueryTestBase<TPTInheritanceQueryLibSqlFixture>(fixture, testOutputHelper)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());
}

public class TPTInheritanceQueryLibSqlFixture : TPTInheritanceQueryFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => LibSqlTestStoreFactory.Instance;
}

public class TPCInheritanceQueryLibSqlTest(TPCInheritanceQueryLibSqlFixture fixture, ITestOutputHelper testOutputHelper)
    : TPCInheritanceQueryTestBase<TPCInheritanceQueryLibSqlFixture>(fixture, testOutputHelper)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());
}

public class TPCInheritanceQueryLibSqlFixture : TPCInheritanceQueryFixture
{
    protected override ITestStoreFactory TestStoreFactory
        => LibSqlTestStoreFactory.Instance;

    public override bool UseGeneratedKeys
        => false;
}
