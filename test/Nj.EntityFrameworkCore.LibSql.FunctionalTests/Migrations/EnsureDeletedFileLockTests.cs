using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

/// <summary>
/// Regression for C-005: EnsureDeleted must clear Exists() even when Windows
/// cannot File.Delete the local db after Close.
/// </summary>
public sealed class EnsureDeletedFileLockTests
{
    [Fact]
    public async Task EnsureDeleted_makes_Exists_false_even_if_file_locked()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-close-" + Guid.NewGuid().ToString("N") + ".db");
        var cs = $"Data Source={path}";
        try
        {
            await using (var context = new MigrationDbContext(MigrationTestHelpers.Configure(cs).Options))
            {
                await context.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
                try
                {
                    await MigrationTestHelpers.EnsureSchemaViaCreateAsync(
                        context,
                        TestContext.Current.CancellationToken);
                }
                finally
                {
                    await context.Database.CloseConnectionAsync();
                }
            }

            await using (var context = new MigrationDbContext(MigrationTestHelpers.Configure(cs).Options))
            {
                await context.Database.EnsureDeletedAsync(TestContext.Current.CancellationToken);
                var creator = context.GetService<IRelationalDatabaseCreator>();
                Assert.False(creator.Exists());
            }

            await using (var verify = new MigrationDbContext(MigrationTestHelpers.Configure(cs).Options))
            {
                await verify.Database.OpenConnectionAsync(TestContext.Current.CancellationToken);
                try
                {
                    Assert.Equal(
                        0,
                        await MigrationTestHelpers.TableCountAsync(
                            verify,
                            "Widgets",
                            TestContext.Current.CancellationToken));
                }
                finally
                {
                    await verify.Database.CloseConnectionAsync();
                }
            }
        }
        finally
        {
            try
            {
                LibSqlConnection.ClearPool(new LibSqlConnection(cs));
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
