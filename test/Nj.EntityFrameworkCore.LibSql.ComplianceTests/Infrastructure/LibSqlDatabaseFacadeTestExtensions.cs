namespace Microsoft.EntityFrameworkCore;

#nullable disable

using Microsoft.EntityFrameworkCore.TestUtilities;

public static class LibSqlDatabaseFacadeTestExtensions
{
    public static void EnsureClean(this DatabaseFacade databaseFacade)
        => new LibSqlDatabaseCleaner().Clean(databaseFacade);
}
