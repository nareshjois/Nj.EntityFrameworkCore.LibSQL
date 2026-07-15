using Microsoft.EntityFrameworkCore;
using MigrationsSample;
using Nj.EntityFrameworkCore.LibSql;

Console.WriteLine(LibSqlProviderInfo.GetScaffoldStatus());

await using var context = new SampleDbContextFactory().CreateDbContext(args);

if (args is ["--apply-seed"])
{
    var schemaPath = Path.GetFullPath(
        Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "seed", "schema.sql"));
    if (!File.Exists(schemaPath))
    {
        schemaPath = Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "seed", "schema.sql"));
    }

    if (!File.Exists(schemaPath))
    {
        Console.Error.WriteLine("Could not find seed/schema.sql");
        return 1;
    }

    await context.Database.OpenConnectionAsync();
    foreach (var statement in File.ReadAllText(schemaPath)
                 .Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries))
    {
        var sql = string.Join(
            '\n',
            statement.Split('\n')
                .Select(l => l.TrimEnd())
                .Where(l => l.Length > 0 && !l.TrimStart().StartsWith("--", StringComparison.Ordinal)));
        if (sql.Length == 0)
        {
            continue;
        }

        await context.Database.ExecuteSqlRawAsync(sql);
    }

    await context.Database.CloseConnectionAsync();
    Console.WriteLine("Applied seed schema from " + schemaPath);
    return 0;
}

Console.WriteLine("Connection: " + SampleDbContextFactory.ResolveConnectionString());
await context.Database.MigrateAsync();

if (!await context.Blogs.AnyAsync())
{
    context.Blogs.Add(
        new Blog
        {
            Url = "https://example.com",
            Posts = [new Post { Title = "Hello libSQL" }]
        });
    await context.SaveChangesAsync();
}

var count = await context.Posts.CountAsync();
Console.WriteLine($"MigrationsSample ready. Posts={count}");
return 0;
