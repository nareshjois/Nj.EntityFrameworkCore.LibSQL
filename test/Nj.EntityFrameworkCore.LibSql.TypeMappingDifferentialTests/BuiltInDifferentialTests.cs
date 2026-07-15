using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.TypeMappingDifferentialTests;

public sealed class BuiltInDifferentialTests
{
    [Fact]
    public async Task Parameter_round_trip_matches_ef_sqlite_for_core_clr_types()
    {
        var seed = CreateRow();

        var sqlitePath = Path.Combine(Path.GetTempPath(), "nj-diff-sqlite-" + Guid.NewGuid().ToString("N") + ".db");
        var libSqlPath = Path.Combine(Path.GetTempPath(), "nj-diff-libsql-" + Guid.NewGuid().ToString("N") + ".db");

        DiffRow fromSqlite;
        DiffRow fromLibSql;

        await using (var sqlite = new DiffDbContext(
                         new DbContextOptionsBuilder<DiffDbContext>()
                             .UseSqlite($"Data Source={sqlitePath}")
                             .Options))
        {
            await sqlite.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            sqlite.Rows.Add(Clone(seed));
            await sqlite.SaveChangesAsync(TestContext.Current.CancellationToken);
            sqlite.ChangeTracker.Clear();
            fromSqlite = await sqlite.Rows.SingleAsync(TestContext.Current.CancellationToken);
        }

        await using (var libSql = new DiffDbContext(
                         new DbContextOptionsBuilder<DiffDbContext>()
                             .UseLibSql($"Data Source={libSqlPath}")
                             .Options))
        {
            await libSql.Database.EnsureCreatedAsync(TestContext.Current.CancellationToken);
            libSql.Rows.Add(Clone(seed));
            await libSql.SaveChangesAsync(TestContext.Current.CancellationToken);
            libSql.ChangeTracker.Clear();
            fromLibSql = await libSql.Rows.SingleAsync(TestContext.Current.CancellationToken);
        }

        Assert.Equal(fromSqlite.Flag, fromLibSql.Flag);
        Assert.Equal(fromSqlite.ByteValue, fromLibSql.ByteValue);
        Assert.Equal(fromSqlite.IntValue, fromLibSql.IntValue);
        Assert.Equal(fromSqlite.LongValue, fromLibSql.LongValue);
        Assert.Equal(fromSqlite.FloatValue, fromLibSql.FloatValue);
        Assert.Equal(fromSqlite.DoubleValue, fromLibSql.DoubleValue, precision: 10);
        Assert.Equal(fromSqlite.DecimalValue, fromLibSql.DecimalValue);
        Assert.Equal(fromSqlite.TextValue, fromLibSql.TextValue);
        Assert.Equal(fromSqlite.GuidValue, fromLibSql.GuidValue);
        Assert.Equal(fromSqlite.DateTimeValue, fromLibSql.DateTimeValue);
        Assert.Equal(fromSqlite.DateTimeOffsetValue, fromLibSql.DateTimeOffsetValue);
        Assert.Equal(fromSqlite.DateOnlyValue, fromLibSql.DateOnlyValue);
        Assert.Equal(fromSqlite.TimeOnlyValue, fromLibSql.TimeOnlyValue);
        Assert.Equal(fromSqlite.BlobValue, fromLibSql.BlobValue);
        Assert.Null(fromLibSql.NullableText);
        Assert.Null(fromSqlite.NullableText);
    }

    private static DiffRow CreateRow()
        => new()
        {
            Id = 1,
            Flag = true,
            ByteValue = 200,
            IntValue = 42,
            LongValue = 99L,
            FloatValue = 1.25f,
            DoubleValue = Math.PI,
            DecimalValue = 123.45m,
            TextValue = "diff-世界",
            GuidValue = Guid.Parse("aaaaaaaa-bbbb-cccc-dddd-eeeeeeeeeeee"),
            DateTimeValue = new DateTime(2026, 7, 15, 12, 30, 45, DateTimeKind.Unspecified),
            DateTimeOffsetValue = new DateTimeOffset(2026, 7, 15, 12, 30, 45, TimeSpan.FromHours(5.5)),
            DateOnlyValue = new DateOnly(2026, 7, 15),
            TimeOnlyValue = new TimeOnly(12, 30, 45),
            BlobValue = "blob"u8.ToArray(),
            NullableText = null
        };

    private static DiffRow Clone(DiffRow row)
        => new()
        {
            Id = row.Id,
            Flag = row.Flag,
            ByteValue = row.ByteValue,
            IntValue = row.IntValue,
            LongValue = row.LongValue,
            FloatValue = row.FloatValue,
            DoubleValue = row.DoubleValue,
            DecimalValue = row.DecimalValue,
            TextValue = row.TextValue,
            GuidValue = row.GuidValue,
            DateTimeValue = row.DateTimeValue,
            DateTimeOffsetValue = row.DateTimeOffsetValue,
            DateOnlyValue = row.DateOnlyValue,
            TimeOnlyValue = row.TimeOnlyValue,
            BlobValue = row.BlobValue.ToArray(),
            NullableText = row.NullableText
        };

    private sealed class DiffRow
    {
        public int Id { get; set; }
        public bool Flag { get; set; }
        public byte ByteValue { get; set; }
        public int IntValue { get; set; }
        public long LongValue { get; set; }
        public float FloatValue { get; set; }
        public double DoubleValue { get; set; }
        public decimal DecimalValue { get; set; }
        public string TextValue { get; set; } = "";
        public Guid GuidValue { get; set; }
        public DateTime DateTimeValue { get; set; }
        public DateTimeOffset DateTimeOffsetValue { get; set; }
        public DateOnly DateOnlyValue { get; set; }
        public TimeOnly TimeOnlyValue { get; set; }
        public byte[] BlobValue { get; set; } = [];
        public string? NullableText { get; set; }
    }

    private sealed class DiffDbContext(DbContextOptions<DiffDbContext> options) : DbContext(options)
    {
        public DbSet<DiffRow> Rows => Set<DiffRow>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
            => modelBuilder.Entity<DiffRow>().Property(e => e.Id).ValueGeneratedNever();
    }
}
