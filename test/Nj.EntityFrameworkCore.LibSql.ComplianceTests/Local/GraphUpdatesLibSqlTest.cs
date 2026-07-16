namespace Microsoft.EntityFrameworkCore;

#nullable disable

public class GraphUpdatesLibSqlChangedNotificationsTest(
    GraphUpdatesLibSqlChangedNotificationsTest.LibSqlFixture fixture)
    : GraphUpdatesLibSqlTestBase<GraphUpdatesLibSqlChangedNotificationsTest.LibSqlFixture>(fixture)
{
    public class LibSqlFixture : GraphUpdatesLibSqlFixtureBase
    {
        protected override string StoreName => "GraphUpdatesLibSqlChanged";

        protected override void OnModelCreating(ModelBuilder modelBuilder, DbContext context)
        {
            modelBuilder.HasChangeTrackingStrategy(ChangeTrackingStrategy.ChangedNotifications);
            base.OnModelCreating(modelBuilder, context);
        }
    }
}
