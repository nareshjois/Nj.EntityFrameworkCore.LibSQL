using Microsoft.EntityFrameworkCore.Scaffolding;
using Microsoft.EntityFrameworkCore.Scaffolding.Metadata;
using Microsoft.Extensions.Logging;
using Nj.EntityFrameworkCore.LibSql.Design.Internal;

namespace Microsoft.EntityFrameworkCore.TestUtilities;

public class LibSqlDatabaseCleaner : RelationalDatabaseCleaner
{
    protected override IDatabaseModelFactory CreateDatabaseModelFactory(ILoggerFactory loggerFactory)
    {
        var services = new ServiceCollection();
        services.AddEntityFrameworkLibSql();

        new LibSqlDesignTimeServices().ConfigureDesignTimeServices(services);

        return services
            .BuildServiceProvider()
            .GetRequiredService<IDatabaseModelFactory>();
    }

    protected override bool AcceptForeignKey(DatabaseForeignKey foreignKey)
        => false;

    protected override bool AcceptIndex(DatabaseIndex index)
        => false;

    protected override string BuildCustomSql(DatabaseModel databaseModel)
        => "PRAGMA foreign_keys=OFF;";

    protected override string BuildCustomEndingSql(DatabaseModel databaseModel)
        => "PRAGMA foreign_keys=ON;";
}
