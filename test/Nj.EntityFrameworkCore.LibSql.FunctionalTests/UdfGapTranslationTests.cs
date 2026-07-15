using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

public sealed class UdfGapTranslationTests
{
    private const string UdfGapDoc = "docs/udf-gap.md";

    [Fact]
    public void Decimal_arithmetic_fails_at_translation_not_execution()
    {
        using var context = CreateContext();
        var query = context.Items.Where(i => i.Amount + 1m > 0m);

        var ex = Assert.ThrowsAny<Exception>(() => query.ToList());
        AssertContainsUdfGap(ex, "ef_add");
    }

    [Fact]
    public void Regex_IsMatch_fails_at_translation()
    {
        using var context = CreateContext();
        var query = context.Items.Where(i => Regex.IsMatch(i.Name, "^a"));

        var ex = Assert.ThrowsAny<Exception>(() => query.ToList());
        AssertContainsUdfGap(ex, "regexp");
    }

    [Fact]
    public void Decimal_OrderBy_fails_at_translation()
    {
        using var context = CreateContext();
        var query = context.Items.OrderBy(i => i.Amount);

        var ex = Assert.ThrowsAny<Exception>(() => query.ToList());
        AssertContainsUdfGap(ex, "EF_DECIMAL");
    }

    [Fact]
    public void Decimal_Average_fails_at_translation()
    {
        using var context = CreateContext();
        var query = context.Items.Select(i => i.Amount);

        var ex = Assert.ThrowsAny<Exception>(() => query.Average());
        AssertContainsUdfGap(ex, "ef_avg");
    }

    private static void AssertContainsUdfGap(Exception ex, string feature)
    {
        var text = Flatten(ex);
        Assert.Contains(feature, text, StringComparison.Ordinal);
        Assert.Contains(UdfGapDoc, text, StringComparison.Ordinal);
    }

    private static string Flatten(Exception ex)
    {
        var parts = new List<string>();
        for (var current = ex; current != null; current = current.InnerException)
        {
            parts.Add(current.Message);
        }

        return string.Join(Environment.NewLine, parts);
    }

    private static GapDbContext CreateContext()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-udf-" + Guid.NewGuid().ToString("N") + ".db");
        return new GapDbContext(
            new DbContextOptionsBuilder<GapDbContext>()
                .UseLibSql($"Data Source={path}")
                .Options);
    }

    private sealed class GapDbContext(DbContextOptions<GapDbContext> options) : DbContext(options)
    {
        public DbSet<GapItem> Items => Set<GapItem>();
    }

    private sealed class GapItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
        public decimal Amount { get; set; }
    }
}
