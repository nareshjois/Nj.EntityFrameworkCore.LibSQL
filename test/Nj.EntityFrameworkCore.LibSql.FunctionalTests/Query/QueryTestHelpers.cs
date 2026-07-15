using System.Text;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.Logging;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

internal static class QueryTestHelpers
{
    public static string LocalConnectionString()
        => $"Data Source={Path.Combine(Path.GetTempPath(), "nj-libsql-q-" + Guid.NewGuid().ToString("N") + ".db")}";

    public static DbContextOptionsBuilder<QueryDbContext> Configure(
        string connectionString,
        SqlCaptureLogger? sqlCapture = null)
    {
        var builder = new DbContextOptionsBuilder<QueryDbContext>()
            .UseLibSql(connectionString);

        if (sqlCapture is not null)
        {
            builder.LogTo(sqlCapture.Write, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
                .EnableSensitiveDataLogging();
        }

        return builder;
    }

    public static async Task EnsureSchemaAsync(QueryDbContext context, CancellationToken cancellationToken)
    {
        var creator = context.GetService<IRelationalDatabaseCreator>();
        await creator.EnsureCreatedAsync(cancellationToken);

        await using var command = context.Database.GetDbConnection().CreateCommand();
        command.CommandText =
            """SELECT COUNT(*) FROM "sqlite_master" WHERE "type" = 'table' AND "name" = 'Blogs';""";
        var count = Convert.ToInt64(await command.ExecuteScalarAsync(cancellationToken));
        if (count == 0)
        {
            await creator.CreateTablesAsync(cancellationToken);
        }
    }

    public static async Task SeedAsync(QueryDbContext context, CancellationToken cancellationToken)
    {
        await context.Database.ExecuteSqlRawAsync(
            """
            DELETE FROM "Posts";
            DELETE FROM "Blogs";
            """,
            cancellationToken);

        context.ChangeTracker.Clear();

        var alphaId = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var betaId = Guid.Parse("11111111-2222-3333-4444-555555555555");

        var alpha = new Blog
        {
            Title = "Alpha Adventures",
            Rating = 5,
            Score = -3.5,
            PublicId = alphaId,
            CreatedAt = new DateTime(2026, 1, 15, 10, 0, 0, DateTimeKind.Unspecified),
            Token = "tok-a"u8.ToArray(),
            Meta = new BlogMeta { Category = "tech", Hits = 10 },
            Tags = [1, 2, 3],
            Posts =
            [
                new Post { Body = "first post" },
                new Post { Body = "second post" }
            ]
        };

        var beta = new Blog
        {
            Title = "Beta Notes",
            Rating = 2,
            Score = 4.25,
            PublicId = betaId,
            CreatedAt = new DateTime(2025, 6, 1, 8, 30, 0, DateTimeKind.Unspecified),
            Token = "tok-b"u8.ToArray(),
            Meta = new BlogMeta { Category = "life", Hits = 3 },
            Tags = [2, 9],
            Posts = [new Post { Body = "beta only" }]
        };

        context.Blogs.AddRange(alpha, beta);
        await context.SaveChangesAsync(cancellationToken);
        context.ChangeTracker.Clear();
    }

    public static async Task TryDeleteLocalFileAsync(string connectionString)
    {
        const string prefix = "Data Source=";
        var idx = connectionString.IndexOf(prefix, StringComparison.OrdinalIgnoreCase);
        if (idx < 0)
        {
            return;
        }

        var path = connectionString[(idx + prefix.Length)..].Trim();
        var semi = path.IndexOf(';');
        if (semi >= 0)
        {
            path = path[..semi];
        }

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

        await Task.CompletedTask;
    }
}

/// <summary>
/// Captures EF command text from <see cref="DbContextOptionsBuilder.LogTo"/> for thin SQL baselines.
/// </summary>
internal sealed class SqlCaptureLogger
{
    private readonly StringBuilder _buffer = new();

    public void Write(string message)
    {
        if (message.Contains("Executing", StringComparison.Ordinal)
            || message.Contains("CommandText", StringComparison.Ordinal)
            || message.Contains('\''))
        {
            _buffer.AppendLine(message);
        }
    }

    public string Text
        => _buffer.ToString();

    public void Clear()
        => _buffer.Clear();

    public void AssertContainsSql(params string[] fragments)
    {
        var sql = Text;
        Assert.False(string.IsNullOrWhiteSpace(sql), "Expected captured SQL, but the log was empty.");
        foreach (var fragment in fragments)
        {
            Assert.Contains(fragment, sql, StringComparison.OrdinalIgnoreCase);
        }
    }
}
