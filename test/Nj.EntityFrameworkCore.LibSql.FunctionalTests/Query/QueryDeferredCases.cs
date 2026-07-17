using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

/// <summary>
/// Deferred query edge cases (TPH, libSQL function goldens, compiled queries, interceptors).
/// </summary>
internal static class QueryDeferredCases
{
    private static readonly Func<QueryDbContext, int, IAsyncEnumerable<Blog>> BlogsByMinRatingQuery
        = EF.CompileAsyncQuery((QueryDbContext context, int minRating) =>
            context.Blogs.Where(b => b.Rating >= minRating));

    public static async Task Tph_inheritance_query(string connectionString, CancellationToken ct)
    {
        try
        {
            await using var context = new InheritanceQueryDbContext(
                new DbContextOptionsBuilder<InheritanceQueryDbContext>()
                    .UseLibSql(connectionString)
                    .Options);

            await context.Database.OpenConnectionAsync(ct);
            var creator = context.GetService<IRelationalDatabaseCreator>();
            await creator.EnsureCreatedAsync(ct);

            await using var command = context.Database.GetDbConnection().CreateCommand();
            command.CommandText =
                """SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = 'Animals';""";
            var count = Convert.ToInt64(await command.ExecuteScalarAsync(ct));
            if (count == 0)
            {
                await creator.CreateTablesAsync(ct);
            }

            context.Animals.AddRange(
                new Cat { Name = "Mittens", Lives = 9 },
                new Dog { Name = "Rex", Breed = "Lab" });
            await context.SaveChangesAsync(ct);
            context.ChangeTracker.Clear();

            var cats = await context.Animals.OfType<Cat>().ToListAsync(ct);
            var dogs = await context.Animals.OfType<Dog>().ToListAsync(ct);

            Assert.Single(cats);
            Assert.Equal(9, cats[0].Lives);
            Assert.Single(dogs);
            Assert.Equal("Lab", dogs[0].Breed);
        }
        finally
        {
            if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                await QueryTestHelpers.TryDeleteLocalFileAsync(connectionString);
            }
        }
    }

    public static async Task Glob_hex_substr_sql_goldens(
        QueryDbContext context,
        SqlCaptureLogger sql,
        CancellationToken ct)
    {
        sql.Clear();
        var globMatch = await context.Blogs
            .Where(b => EF.Functions.Glob(b.Title, "Alpha*"))
            .Select(b => b.Title)
            .ToListAsync(ct);
        Assert.Equal(["Alpha Adventures"], globMatch);
        sql.AssertContainsSql("glob");

        sql.Clear();
        var hex = await context.Blogs
            .OrderBy(b => b.Id)
            .Select(b => EF.Functions.Hex(b.Token))
            .FirstAsync(ct);
        Assert.False(string.IsNullOrWhiteSpace(hex));
        sql.AssertContainsSql("hex");

        sql.Clear();
        var substr = await context.Blogs
            .OrderBy(b => b.Id)
            .Select(b => EF.Functions.Substr(b.Token, 2, 2))
            .FirstAsync(ct);
        Assert.Equal(2, substr.Length);
        sql.AssertContainsSql("substr");

        sql.Clear();
        var prefix = await context.Blogs
            .OrderBy(b => b.Title)
            .Select(b => b.Title.Substring(0, 5))
            .FirstAsync(ct);
        Assert.Equal("Alpha", prefix);
        sql.AssertContainsSql("substr");
    }

    public static async Task Compiled_query_smoke(QueryDbContext context, CancellationToken ct)
    {
        var titles = new List<string>();
        await foreach (var blog in BlogsByMinRatingQuery(context, 4).WithCancellation(ct))
        {
            titles.Add(blog.Title);
        }

        titles.Sort(StringComparer.Ordinal);
        Assert.Equal(["Alpha Adventures"], titles);
    }

    public static async Task Command_and_query_interceptors(string connectionString, CancellationToken ct)
    {
        var commandInterceptor = new RecordingCommandInterceptor();
        var queryInterceptor = new RecordingQueryExpressionInterceptor();
        try
        {
            await using var context = new QueryDbContext(
                new DbContextOptionsBuilder<QueryDbContext>()
                    .UseLibSql(connectionString)
                    .AddInterceptors(commandInterceptor, queryInterceptor)
                    .Options);

            await context.Database.OpenConnectionAsync(ct);
            await context.Database.EnsureCreatedAsync(ct);
            await QueryTestHelpers.SeedAsync(context, ct);

            _ = await context.Blogs.Select(b => b.Title).ToListAsync(ct);

            Assert.NotEmpty(commandInterceptor.CommandTexts);
            Assert.Contains(commandInterceptor.CommandTexts, t => t.Contains("Blogs", StringComparison.OrdinalIgnoreCase));
            Assert.True(queryInterceptor.QueryCompilationCount >= 1);
        }
        finally
        {
            if (connectionString.StartsWith("Data Source=", StringComparison.OrdinalIgnoreCase))
            {
                await QueryTestHelpers.TryDeleteLocalFileAsync(connectionString);
            }
        }
    }
}
