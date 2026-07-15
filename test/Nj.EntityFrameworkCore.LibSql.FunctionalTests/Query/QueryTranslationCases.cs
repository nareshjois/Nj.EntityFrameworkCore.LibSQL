using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;

/// <summary>
/// Shared LINQ matrix exercised by local and remote hosts.
/// </summary>
internal static class QueryTranslationCases
{
    public static async Task Filter_and_project(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var rows = await context.Blogs
            .Where(b => b.Rating >= 3)
            .Select(b => new { b.Title, b.Rating })
            .ToListAsync(ct);

        Assert.Single(rows);
        Assert.Equal("Alpha Adventures", rows[0].Title);
        sql.AssertContainsSql("WHERE", "Rating");
    }

    public static async Task Order_skip_take(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var titles = await context.Blogs
            .OrderBy(b => b.Rating)
            .Skip(1)
            .Take(1)
            .Select(b => b.Title)
            .ToListAsync(ct);

        Assert.Single(titles);
        Assert.Equal("Alpha Adventures", titles[0]);
        sql.AssertContainsSql("ORDER BY", "LIMIT");
    }

    public static async Task Join(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var rows = await (
            from b in context.Blogs
            join p in context.Posts on b.Id equals p.BlogId
            where b.Title.StartsWith("Alpha")
            select p.Body).ToListAsync(ct);

        Assert.Equal(2, rows.Count);
        Assert.Contains("first post", rows);
        sql.AssertContainsSql("INNER JOIN", "Posts");
    }

    public static async Task Include_collection(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var blog = await context.Blogs
            .Include(b => b.Posts)
            .SingleAsync(b => b.Title == "Alpha Adventures", ct);

        Assert.Equal(2, blog.Posts.Count);
        sql.AssertContainsSql("FROM", "Blogs");
    }

    public static async Task GroupBy_aggregate(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var rows = await context.Posts
            .GroupBy(p => p.BlogId)
            .Select(g => new { BlogId = g.Key, Count = g.Count() })
            .OrderBy(x => x.BlogId)
            .ToListAsync(ct);

        Assert.Equal(2, rows.Count);
        Assert.Equal(2, rows.Single(r => r.Count == 2).Count);
        Assert.Equal(1, rows.Single(r => r.Count == 1).Count);
        sql.AssertContainsSql("GROUP BY");
    }

    public static async Task Union_set_operation(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var a = context.Blogs.Where(b => b.Rating == 5).Select(b => b.Title);
        var b = context.Blogs.Where(b => b.Rating == 2).Select(b => b.Title);
        var titles = await a.Union(b).OrderBy(t => t).ToListAsync(ct);

        Assert.Equal(["Alpha Adventures", "Beta Notes"], titles);
        sql.AssertContainsSql("UNION");
    }

    public static async Task String_methods(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var rows = await context.Blogs
            .Where(b => b.Title.Contains("Alpha") && b.Title.StartsWith("A") && b.Title.ToUpper().Length > 5)
            .Select(b => b.Title.ToUpper())
            .ToListAsync(ct);

        Assert.Single(rows);
        Assert.Equal("ALPHA ADVENTURES", rows[0]);
        sql.AssertContainsSql("LIKE", "length", "upper");
    }

    public static async Task Math_methods(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var scores = await context.Blogs
            .OrderBy(b => b.Id)
            .Select(b => Math.Abs(b.Score))
            .ToListAsync(ct);

        Assert.Equal(2, scores.Count);
        Assert.Equal(3.5, scores[0], precision: 5);
        Assert.Equal(4.25, scores[1], precision: 5);
        sql.AssertContainsSql("abs");
    }

    public static async Task DateTime_member(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var years = await context.Blogs
            .Where(b => b.CreatedAt.Year == 2026)
            .Select(b => b.CreatedAt.Year)
            .ToListAsync(ct);

        Assert.Single(years);
        Assert.Equal(2026, years[0]);
        sql.AssertContainsSql("strftime");
    }

    public static async Task Guid_and_bytes(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var id = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee");
        var blog = await context.Blogs.SingleAsync(b => b.PublicId == id, ct);
        Assert.Equal("Alpha Adventures", blog.Title);
        Assert.Equal(5, blog.Token.Length);

        sql.Clear();
        var lengths = await context.Blogs
            .OrderBy(b => b.Id)
            .Select(b => b.Token.Length)
            .ToListAsync(ct);
        Assert.Equal([5, 5], lengths);
        sql.AssertContainsSql("length");
    }

    public static async Task Json_and_primitive_collection(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var byCategory = await context.Blogs
            .Where(b => b.Meta.Category == "tech")
            .Select(b => b.Title)
            .ToListAsync(ct);
        Assert.Equal(["Alpha Adventures"], byCategory);

        sql.Clear();
        var withTag = await context.Blogs
            .Where(b => b.Tags.Contains(9))
            .Select(b => b.Title)
            .ToListAsync(ct);
        Assert.Equal(["Beta Notes"], withTag);
        sql.AssertContainsSql("json");
    }

    public static async Task FromSql_interpolated(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        var minRating = 4;
        var rows = await context.Blogs
            .FromSqlInterpolated($"""SELECT * FROM "Blogs" WHERE "Rating" >= {minRating}""")
            .AsNoTracking()
            .Select(b => b.Title)
            .ToListAsync(ct);

        Assert.Equal(["Alpha Adventures"], rows);
        sql.AssertContainsSql("Rating");
    }

    public static async Task Sync_and_async_parity(QueryDbContext context, CancellationToken ct)
    {
        var asyncTitles = await context.Blogs.OrderBy(b => b.Title).Select(b => b.Title).ToListAsync(ct);
        var syncTitles = context.Blogs.OrderBy(b => b.Title).Select(b => b.Title).ToList();
        Assert.Equal(asyncTitles, syncTitles);
    }

    public static async Task TagWith(QueryDbContext context, SqlCaptureLogger sql, CancellationToken ct)
    {
        sql.Clear();
        _ = await context.Blogs
            .TagWith("wp06-query")
            .Select(b => b.Id)
            .ToListAsync(ct);
        sql.AssertContainsSql("wp06-query");
    }
}
