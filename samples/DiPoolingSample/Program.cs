using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Nj.EntityFrameworkCore.LibSql;
using Nj.LibSql.Data;

// DiPoolingSample — AddDbContextPool, IDbContextFactory, and UseLibSql(existing connection).

var path = Path.Combine(Path.GetTempPath(), "nj-dipooling-" + Guid.NewGuid().ToString("N") + ".db");
var connectionString = $"Data Source={path}";

Console.WriteLine(LibSqlProviderInfo.GetScaffoldStatus());

try
{
    // --- 1) DI + pooled DbContext ---
    {
        var services = new ServiceCollection();
        services.AddDbContextPool<SampleContext>(options => options.UseLibSql(connectionString));
        await using var provider = services.BuildServiceProvider();
        await using var scope = provider.CreateAsyncScope();
        var db = scope.ServiceProvider.GetRequiredService<SampleContext>();
        await db.Database.EnsureCreatedAsync();
        db.Notes.Add(new Note { Text = "from-pool" });
        await db.SaveChangesAsync();
        Console.WriteLine("AddDbContextPool: SaveChanges OK");
    }

    // --- 2) IDbContextFactory ---
    {
        var services = new ServiceCollection();
        services.AddDbContextFactory<SampleContext>(options => options.UseLibSql(connectionString));
        await using var provider = services.BuildServiceProvider();
        var factory = provider.GetRequiredService<IDbContextFactory<SampleContext>>();
        await using var db = await factory.CreateDbContextAsync();
        db.Notes.Add(new Note { Text = "from-factory" });
        await db.SaveChangesAsync();
        Console.WriteLine("IDbContextFactory: SaveChanges OK");
    }

    // --- 3) Existing LibSqlConnection (caller owns lifetime) ---
    await using (var connection = new LibSqlConnection(connectionString))
    {
        await connection.OpenAsync();
        await using var db = new SampleContext(
            new DbContextOptionsBuilder<SampleContext>()
                .UseLibSql(connection)
                .Options);
        db.Notes.Add(new Note { Text = "from-existing-connection" });
        await db.SaveChangesAsync();
        Console.WriteLine("UseLibSql(existing connection): SaveChanges OK");
    }
}
finally
{
    try
    {
        LibSqlConnection.ClearAllPools();
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

file sealed class SampleContext(DbContextOptions<SampleContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

file sealed class Note
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
}
