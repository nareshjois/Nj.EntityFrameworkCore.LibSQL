using Microsoft.EntityFrameworkCore.TestModels.UpdatesModel;
using Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;

namespace Microsoft.EntityFrameworkCore.Update;

#nullable disable

public class UpdatesLibSqlTest(UpdatesLibSqlTest.UpdatesLibSqlFixture fixture)
    : UpdatesRelationalTestBase<UpdatesLibSqlTest.UpdatesLibSqlFixture>(fixture)
{
    public override Task Save_with_shared_foreign_key()
        => Task.CompletedTask;

    public override void Identifiers_are_generated_correctly()
    {
        using var context = CreateContext();
        var entityType = context.Model.FindEntityType(
            typeof(
                LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameThatIsUsedToVerifyThatTheStoreIdentifierGenerationLengthLimitIsWorkingCorrectly
            ));
        Assert.Equal(
            "LoginEntityTypeWithAnExtremelyLongAndOverlyConvolutedNameThatIsUsedToVerifyThatTheStoreIdentifierGenerationLengthLimitIsWorkingCorrectly",
            entityType.GetTableName());
    }

    public class UpdatesLibSqlFixture : UpdatesRelationalFixture
    {
        protected override ITestStoreFactory TestStoreFactory
            => LibSqlTestStoreFactory.Instance;
    }
}
