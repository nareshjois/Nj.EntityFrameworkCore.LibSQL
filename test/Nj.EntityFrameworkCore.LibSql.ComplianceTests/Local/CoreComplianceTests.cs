using Microsoft.EntityFrameworkCore.Diagnostics;
using Nj.EntityFrameworkCore.LibSql.ComplianceTests.Fixtures;
using Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;
using Xunit.Abstractions;

namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class BuiltInDataTypesLibSqlTest
    : BuiltInDataTypesTestBase<BuiltInDataTypesLibSqlTest.BuiltInDataTypesLibSqlFixture>
{
    public BuiltInDataTypesLibSqlTest(BuiltInDataTypesLibSqlFixture fixture, ITestOutputHelper testOutputHelper)
        : base(fixture)
    {
        fixture.TestSqlLoggerFactory.Clear();
        fixture.TestSqlLoggerFactory.SetTestOutputHelper(testOutputHelper);
    }

    public class BuiltInDataTypesLibSqlFixture : BuiltInDataTypesFixtureBase, ITestSqlLoggerFactory
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
            => LibSqlTestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;
    }
}

public class TransactionLibSqlTest(TransactionLibSqlTest.TransactionLibSqlFixture fixture)
    : TransactionTestBase<TransactionLibSqlTest.TransactionLibSqlFixture>(fixture)
{
    protected override bool SnapshotSupported => false;

    // libSQL does not implement SQLite shared-cache mode, so a second connection cannot
    // observe another connection's uncommitted writes even with PRAGMA read_uncommitted.
    protected override bool DirtyReadsOccur => false;

    protected override DbContext CreateContextWithConnectionString()
    {
        var options = Fixture.AddOptions(
                new DbContextOptionsBuilder().UseLibSql(TestStore.ConnectionString)
                    .ConfigureWarnings(w => w.Log(RelationalEventId.AmbientTransactionWarning)))
            .UseInternalServiceProvider(Fixture.ServiceProvider);

        return new DbContext(options.Options);
    }

    public class TransactionLibSqlFixture : TransactionFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => SharedCacheLibSqlTestStoreFactory.Instance;

        public override async Task ReseedAsync()
        {
            using var context = CreateContext();
            context.Set<TransactionCustomer>().RemoveRange(await context.Set<TransactionCustomer>().ToListAsync());
            context.Set<TransactionOrder>().RemoveRange(await context.Set<TransactionOrder>().ToListAsync());
            await context.SaveChangesAsync();

            await SeedAsync(context);
        }

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => base.AddOptions(builder)
                .ConfigureWarnings(w => w.Log(RelationalEventId.AmbientTransactionWarning));
    }
}

public class StoreGeneratedLibSqlTest(StoreGeneratedLibSqlTest.StoreGeneratedLibSqlFixture fixture)
    : StoreGeneratedTestBase<StoreGeneratedLibSqlTest.StoreGeneratedLibSqlFixture>(fixture)
{
    public override Task Fields_used_correctly_for_store_generated_values()
        => Task.CompletedTask;

    [ConditionalFact]
    public Task Identity_key_works_when_not_aliasing_rowid()
        => ExecuteWithStrategyInTransactionAsync(async context =>
        {
            var entry = context.Add(new Zach());

            await context.SaveChangesAsync();
            var id = entry.Entity.Id;

            Assert.Equal(16, id?.Length ?? 0);
        });

    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class StoreGeneratedLibSqlFixture : StoreGeneratedFixtureBase
    {
        protected override string StoreName => "StoreGeneratedLibSqlTest";

        protected override ITestStoreFactory TestStoreFactory
            => LibSqlTestStoreFactory.Instance;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => builder
                .EnableSensitiveDataLogging()
                .ConfigureWarnings(b => b.Default(WarningBehavior.Throw)
                    .Ignore(CoreEventId.SensitiveDataLoggingEnabledWarning)
                    .Ignore(RelationalEventId.BoolWithDefaultWarning));

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            modelBuilder.Entity<Gumball>(b =>
            {
                b.Property(e => e.Identity).HasDefaultValue("Banana Joe");
                b.Property(e => e.IdentityReadOnlyBeforeSave).HasDefaultValue("Doughnut Sheriff");
                b.Property(e => e.IdentityReadOnlyAfterSave).HasDefaultValue("Anton");
                b.Property(e => e.AlwaysIdentity).HasDefaultValue("Banana Joe");
                b.Property(e => e.AlwaysIdentityReadOnlyBeforeSave).HasDefaultValue("Doughnut Sheriff");
                b.Property(e => e.AlwaysIdentityReadOnlyAfterSave).HasDefaultValue("Anton");
                b.Property(e => e.Computed).HasDefaultValue("Alan");
                b.Property(e => e.ComputedReadOnlyBeforeSave).HasDefaultValue("Carmen");
                b.Property(e => e.ComputedReadOnlyAfterSave).HasDefaultValue("Tina Rex");
                b.Property(e => e.AlwaysComputed).HasDefaultValue("Alan");
                b.Property(e => e.AlwaysComputedReadOnlyBeforeSave).HasDefaultValue("Carmen");
                b.Property(e => e.AlwaysComputedReadOnlyAfterSave).HasDefaultValue("Tina Rex");
            });

            modelBuilder.Entity<Anais>(b =>
            {
                b.Property(e => e.OnAdd).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddUseBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddIgnoreBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddThrowBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddUseBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddIgnoreBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddThrowBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddUseBeforeThrowAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddIgnoreBeforeThrowAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddThrowBeforeThrowAfter).HasDefaultValue("Rabbit");

                b.Property(e => e.OnAddOrUpdate).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateUseBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateIgnoreBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateThrowBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateUseBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateIgnoreBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateThrowBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateUseBeforeThrowAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateIgnoreBeforeThrowAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnAddOrUpdateThrowBeforeThrowAfter).HasDefaultValue("Rabbit");

                b.Property(e => e.OnUpdate).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateUseBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateIgnoreBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateThrowBeforeUseAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateUseBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateIgnoreBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateThrowBeforeIgnoreAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateUseBeforeThrowAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateIgnoreBeforeThrowAfter).HasDefaultValue("Rabbit");
                b.Property(e => e.OnUpdateThrowBeforeThrowAfter).HasDefaultValue("Rabbit");
            });

            modelBuilder.Entity<WithNoBackingFields>(b =>
            {
                b.Property(e => e.TrueDefault).HasDefaultValue(true);
                b.Property(e => e.NonZeroDefault).HasDefaultValue(-1);
                b.Property(e => e.FalseDefault).HasDefaultValue(false);
                b.Property(e => e.ZeroDefault).HasDefaultValue(0);
            });

            modelBuilder.Entity<WithNullableBackingFields>(b =>
            {
                b.Property(e => e.NullableBackedBoolTrueDefault).HasDefaultValue(true);
                b.Property(e => e.NullableBackedIntNonZeroDefault).HasDefaultValue(-1);
                b.Property(e => e.NullableBackedBoolFalseDefault).HasDefaultValue(false);
                b.Property(e => e.NullableBackedIntZeroDefault).HasDefaultValue(0);
            });

            modelBuilder.Entity<WithObjectBackingFields>(b =>
            {
                b.Property(e => e.NullableBackedBoolTrueDefault).HasDefaultValue(true);
                b.Property(e => e.NullableBackedIntNonZeroDefault).HasDefaultValue(-1);
                b.Property(e => e.NullableBackedBoolFalseDefault).HasDefaultValue(false);
                b.Property(e => e.NullableBackedIntZeroDefault).HasDefaultValue(0);
            });

            modelBuilder.Entity<Zach>().Property(e => e.Id)
                .ValueGeneratedOnAdd()
                .HasDefaultValueSql("randomblob(16)");

            modelBuilder.Entity<NonStoreGenDependent>().Property(e => e.HasTemp).HasDefaultValue(777);

            base.OnModelCreating(modelBuilder, context);
        }
    }

    private class Zach
    {
        public byte[] Id { get; set; }
    }
}

public class OptimisticConcurrencyLibSqlTest(F1ULongLibSqlFixture fixture)
    : OptimisticConcurrencyLibSqlTestBase<F1ULongLibSqlFixture, ulong?>(fixture);

public abstract class OptimisticConcurrencyLibSqlTestBase<TFixture, TRowVersion>(TFixture fixture)
    : OptimisticConcurrencyRelationalTestBase<TFixture, TRowVersion>(fixture)
    where TFixture : F1RelationalFixture<TRowVersion>, new()
{
    // C-014 / Sqlite-parity: EF #2195 optimistic offline lock — no DB rowversion / auto token bump,
    // so these paths never raise DbUpdateConcurrencyException (same skips as EF Sqlite).
    // Duplicate-insert / M2M association cases are not skipped; soft-fork UNIQUE surfacing covers them.
    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Simple_concurrency_exception_can_be_resolved_with_client_values()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Simple_concurrency_exception_can_be_resolved_with_new_values()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values_using_equivalent_of_accept_changes()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Simple_concurrency_exception_can_be_resolved_with_store_values_using_Reload()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task
        Updating_then_deleting_the_same_entity_results_in_DbUpdateConcurrencyException_which_can_be_resolved_with_store_values()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task
        Change_in_independent_association_after_change_in_different_concurrency_token_results_in_independent_association_exception()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Change_in_independent_association_results_in_independent_association_exception()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Two_concurrency_issues_in_one_to_many_related_entities_can_be_handled_by_dealing_with_dependent_first()
        => Task.CompletedTask;

    [ConditionalFact(Skip = "C-014: Optimistic Offline Lock (Sqlite #2195 parity).")]
    public override Task Two_concurrency_issues_in_one_to_one_related_entities_can_be_handled_by_dealing_with_dependent_first()
        => Task.CompletedTask;

    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());
}

public class ValueConvertersEndToEndLibSqlTest(ValueConvertersEndToEndLibSqlTest.LibSqlFixture fixture)
    : ValueConvertersEndToEndTestBase<ValueConvertersEndToEndLibSqlTest.LibSqlFixture>(fixture)
{
    public class LibSqlFixture : ValueConvertersEndToEndFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => LibSqlTestStoreFactory.Instance;
    }
}

public class WithConstructorsLibSqlTest(WithConstructorsLibSqlTest.LibSqlFixture fixture)
    : WithConstructorsTestBase<WithConstructorsLibSqlTest.LibSqlFixture>(fixture)
{
    protected override void UseTransaction(DatabaseFacade facade, IDbContextTransaction transaction)
        => facade.UseTransaction(transaction.GetDbTransaction());

    public class LibSqlFixture : WithConstructorsFixtureBase
    {
        protected override ITestStoreFactory TestStoreFactory
            => LibSqlTestStoreFactory.Instance;

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            base.OnModelCreating(modelBuilder, context);

            modelBuilder.Entity<BlogQuery>().HasNoKey().ToSqlQuery("SELECT * FROM Blog");
        }
    }
}

public class ModelBuilding101LibSqlTest : ModelBuilding101RelationalTestBase
{
    protected override DbContextOptionsBuilder ConfigureContext(DbContextOptionsBuilder optionsBuilder)
        => optionsBuilder.UseLibSql("Data Source=:memory:");
}

public class ConcurrencyDetectorEnabledLibSqlTest(
    ConcurrencyDetectorEnabledLibSqlTest.ConcurrencyDetectorLibSqlFixture fixture)
    : ConcurrencyDetectorEnabledRelationalTestBase<ConcurrencyDetectorEnabledLibSqlTest.ConcurrencyDetectorLibSqlFixture>(
        fixture)
{
    public class ConcurrencyDetectorLibSqlFixture : ConcurrencyDetectorFixtureBase, ITestSqlLoggerFactory
    {
        protected override ITestStoreFactory TestStoreFactory
            => LibSqlTestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;
    }
}

public class ConcurrencyDetectorDisabledLibSqlTest(
    ConcurrencyDetectorDisabledLibSqlTest.ConcurrencyDetectorLibSqlFixture fixture)
    : ConcurrencyDetectorDisabledRelationalTestBase<ConcurrencyDetectorDisabledLibSqlTest.ConcurrencyDetectorLibSqlFixture>(
        fixture)
{
    public class ConcurrencyDetectorLibSqlFixture : ConcurrencyDetectorFixtureBase, ITestSqlLoggerFactory
    {
        protected override ITestStoreFactory TestStoreFactory
            => LibSqlTestStoreFactory.Instance;

        public TestSqlLoggerFactory TestSqlLoggerFactory
            => (TestSqlLoggerFactory)ListLoggerFactory;

        public override DbContextOptionsBuilder AddOptions(DbContextOptionsBuilder builder)
            => builder.EnableThreadSafetyChecks(enableChecks: false);
    }
}
