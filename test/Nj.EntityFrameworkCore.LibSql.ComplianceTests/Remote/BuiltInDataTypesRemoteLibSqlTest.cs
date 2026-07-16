using Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore;

[Collection(RemoteLibSqlComplianceCollection.Name)]
public sealed class BuiltInDataTypesRemoteLibSqlTest
    : BuiltInDataTypesTestBase<BuiltInDataTypesRemoteLibSqlTest.BuiltInDataTypesRemoteLibSqlFixture>
{
    public BuiltInDataTypesRemoteLibSqlTest(
        BuiltInDataTypesRemoteLibSqlFixture fixture,
        RemoteLibSqlComplianceFixture remote,
        ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        RemoteComplianceAssert.SkipIfUnavailable(remote);
        fixture.TestSqlLoggerFactory.Clear();
        fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    public class BuiltInDataTypesRemoteLibSqlFixture : BuiltInDataTypesFixtureBase, ITestSqlLoggerFactory
    {
        public override bool StrictEquality => false;
        public override bool SupportsAnsi => false;
        public override bool SupportsUnicodeToAnsiConversion => true;
        public override bool SupportsLargeStringComparisons => true;
        public override bool SupportsDecimalComparisons => false;
        public override bool PreservesDateTimeKind => false;
        public override bool SupportsBinaryKeys => true;
        public override DateTime DefaultDateTime => new(2000, 1, 1, 0, 0, 0, DateTimeKind.Unspecified);

        protected override ITestStoreFactory TestStoreFactory
            => RemoteLibSqlTestStoreFactorySingleton.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;
    }
}
