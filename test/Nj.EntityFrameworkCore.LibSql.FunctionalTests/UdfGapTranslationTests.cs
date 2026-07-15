using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Logging;
using Nj.EntityFrameworkCore.LibSql.FunctionalTests.Query;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests;

public sealed class UdfGapTranslationTests
{
    [Fact]
    public void Decimal_arithmetic_translates_via_real_and_round_trips()
    {
        using var context = CreateSeededContext(out var sql);
        var rows = context.Items
            .Where(i => i.Amount + 1m > 10m)
            .Select(i => i.Amount * 2m)
            .ToList();

        Assert.Equal([20m], rows);
        sql.AssertContainsSql("CAST");
        Assert.DoesNotContain("ef_add", sql.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ef_multiply", sql.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decimal_negate_and_modulo_translate_via_real()
    {
        using var context = CreateSeededContext(out var sql);
        var rows = context.Items
            .Where(i => i.Amount == 10m)
            .Select(i => new { Neg = -i.Amount, Mod = i.Amount % 3m })
            .ToList();

        Assert.Single(rows);
        Assert.Equal(-10m, rows[0].Neg);
        Assert.Equal(1m, rows[0].Mod);
        Assert.DoesNotContain("ef_negate", sql.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ef_mod", sql.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decimal_OrderBy_uses_numeric_real_order()
    {
        using var context = CreateSeededContext(out var sql);
        // Lexicographic TEXT order would put 9 after 10; REAL cast must put 9 before 10.
        var ordered = context.Items.OrderBy(i => i.Amount).Select(i => i.Amount).ToList();

        Assert.Equal([9m, 10m], ordered);
        sql.AssertContainsSql("ORDER BY");
        Assert.DoesNotContain("EF_DECIMAL", sql.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Decimal_aggregates_translate_via_real()
    {
        using var context = CreateSeededContext(out var sql);
        var amounts = context.Items.Select(i => i.Amount);

        Assert.Equal(9.5m, amounts.Average());
        Assert.Equal(19m, amounts.Sum());
        Assert.Equal(9m, amounts.Min());
        Assert.Equal(10m, amounts.Max());

        Assert.DoesNotContain("ef_avg", sql.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ef_sum", sql.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ef_min", sql.Text, StringComparison.OrdinalIgnoreCase);
        Assert.DoesNotContain("ef_max", sql.Text, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void Regex_IsMatch_translates_to_native_regexp()
    {
        using var context = CreateSeededContext(out var sql);
        var names = context.Items.Where(i => Regex.IsMatch(i.Name, "^t")).Select(i => i.Name).ToList();

        Assert.Equal(["ten"], names);
        sql.AssertContainsSql("REGEXP");
    }

    private static GapDbContext CreateSeededContext(out SqlCaptureLogger sql)
    {
        sql = new SqlCaptureLogger();
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-udf-" + Guid.NewGuid().ToString("N") + ".db");
        var context = new GapDbContext(
            new DbContextOptionsBuilder<GapDbContext>()
                .UseLibSql($"Data Source={path}")
                .LogTo(sql.Write, [DbLoggerCategory.Database.Command.Name], LogLevel.Information)
                .EnableSensitiveDataLogging()
                .Options);

        context.Database.EnsureCreated();
        context.Items.AddRange(
            new GapItem { Name = "nine", Amount = 9m },
            new GapItem { Name = "ten", Amount = 10m });
        context.SaveChanges();
        context.ChangeTracker.Clear();
        sql.Clear();
        return context;
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
