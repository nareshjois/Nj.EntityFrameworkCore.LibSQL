using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

public sealed class GeneratedKeySaveChangesTests
{
    [Fact]
    public async Task SaveChanges_persists_database_generated_integer_keys()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-gen-" + Guid.NewGuid().ToString("N") + ".db");
        try
        {
            await using var context = new GenDbContext(
                new DbContextOptionsBuilder<GenDbContext>()
                    .UseLibSql($"Data Source={path}")
                    .Options);

            await context.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            context.Items.Add(new GenItem { Name = "ada" });
            Assert.Equal(1, await context.SaveChangesAsync(TestContext.Current.CancellationToken));
            context.ChangeTracker.Clear();

            var loaded = await context.Items.SingleAsync(TestContext.Current.CancellationToken);
            Assert.Equal(1, loaded.Id);
            Assert.Equal("ada", loaded.Name);
            Assert.False(File.Exists(path + "-journal"));
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // Best-effort cleanup.
            }
        }
    }

    private sealed class GenItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }

    private sealed class GenDbContext(DbContextOptions<GenDbContext> options) : DbContext(options)
    {
        public DbSet<GenItem> Items => Set<GenItem>();
    }
}
