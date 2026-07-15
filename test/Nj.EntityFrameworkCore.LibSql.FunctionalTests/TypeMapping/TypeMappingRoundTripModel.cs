using Microsoft.EntityFrameworkCore;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.TypeMapping;

public enum SampleStatus
{
    Pending = 0,
    Active = 1,
    Closed = 2
}

public sealed class BuiltInRow
{
    public int Id { get; set; }

    public bool Flag { get; set; }
    public byte ByteValue { get; set; }
    public short ShortValue { get; set; }
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
    public SampleStatus Status { get; set; }
    public string? NullableText { get; set; }
    public int? NullableInt { get; set; }
}

public sealed class ConverterKeyRow
{
    public Guid Id { get; set; }
    public string Name { get; set; } = "";
}

public sealed class JsonPayload
{
    public string Label { get; set; } = "";
    public int Count { get; set; }
}

public sealed class JsonRow
{
    public int Id { get; set; }
    public JsonPayload Payload { get; set; } = new();
    public List<int> Numbers { get; set; } = [];
}

public sealed class DefaultRow
{
    public int Id { get; set; }
    public string Name { get; set; } = "";
    public int Counter { get; set; }
}

public sealed class TypeMappingDbContext : DbContext
{
    public TypeMappingDbContext(DbContextOptions<TypeMappingDbContext> options)
        : base(options)
    {
    }

    public DbSet<BuiltInRow> BuiltIns => Set<BuiltInRow>();
    public DbSet<ConverterKeyRow> ConverterKeys => Set<ConverterKeyRow>();
    public DbSet<JsonRow> JsonRows => Set<JsonRow>();
    public DbSet<DefaultRow> Defaults => Set<DefaultRow>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Database-generated keys via INSERT…RETURNING (or INSERT+SELECT last_insert_rowid)
        // do not persist correctly with Nelknet 0.2.10 under EF SaveChanges — deferred to WP-07.
        // Round-trips use client-assigned keys so type mapping can be validated independently.
        modelBuilder.Entity<BuiltInRow>(e => e.Property(x => x.Id).ValueGeneratedNever());
        modelBuilder.Entity<JsonRow>(e => e.Property(x => x.Id).ValueGeneratedNever());
        modelBuilder.Entity<DefaultRow>(e => e.Property(x => x.Id).ValueGeneratedNever());

        modelBuilder.Entity<ConverterKeyRow>(e =>
        {
            e.Property(x => x.Id)
                .HasConversion(
                    g => g.ToString("D"),
                    s => Guid.Parse(s));
        });

        modelBuilder.Entity<JsonRow>(e =>
        {
            e.OwnsOne(x => x.Payload, p => p.ToJson());
            e.PrimitiveCollection(x => x.Numbers);
        });

        modelBuilder.Entity<DefaultRow>(e =>
        {
            e.Property(x => x.Counter).HasDefaultValue(42);
            e.Property(x => x.Name).HasDefaultValue("anon");
        });
    }
}

public static class TypeMappingSampleData
{
    public static BuiltInRow CreateBuiltIn(int id = 1)
        => new()
        {
            Id = id,
            Flag = true,
            ByteValue = 200,
            ShortValue = -1234,
            IntValue = 1_000_001,
            LongValue = 9_007_199_254_740_991L,
            FloatValue = 1.25f,
            DoubleValue = Math.PI,
            DecimalValue = 1234567890.12345m,
            TextValue = "hello-世界",
            GuidValue = Guid.Parse("11111111-2222-3333-4444-555555555555"),
            DateTimeValue = new DateTime(2026, 7, 15, 12, 30, 45, DateTimeKind.Unspecified),
            DateTimeOffsetValue = new DateTimeOffset(2026, 7, 15, 12, 30, 45, TimeSpan.FromHours(5.5)),
            DateOnlyValue = new DateOnly(2026, 7, 15),
            TimeOnlyValue = new TimeOnly(12, 30, 45),
            BlobValue = "blob-世界"u8.ToArray(),
            Status = SampleStatus.Active,
            NullableText = null,
            NullableInt = null
        };

    public static void AssertBuiltInEqual(BuiltInRow expected, BuiltInRow actual)
    {
        Assert.Equal(expected.Flag, actual.Flag);
        Assert.Equal(expected.ByteValue, actual.ByteValue);
        Assert.Equal(expected.ShortValue, actual.ShortValue);
        Assert.Equal(expected.IntValue, actual.IntValue);
        Assert.Equal(expected.LongValue, actual.LongValue);
        Assert.Equal(expected.FloatValue, actual.FloatValue);
        Assert.Equal(expected.DoubleValue, actual.DoubleValue, precision: 10);
        Assert.Equal(expected.DecimalValue, actual.DecimalValue);
        Assert.Equal(expected.TextValue, actual.TextValue);
        Assert.Equal(expected.GuidValue, actual.GuidValue);
        Assert.Equal(expected.DateTimeValue, actual.DateTimeValue);
        Assert.Equal(expected.DateTimeOffsetValue, actual.DateTimeOffsetValue);
        Assert.Equal(expected.DateOnlyValue, actual.DateOnlyValue);
        Assert.Equal(expected.TimeOnlyValue, actual.TimeOnlyValue);
        Assert.Equal(expected.BlobValue, actual.BlobValue);
        Assert.Equal(expected.Status, actual.Status);
        Assert.Null(actual.NullableText);
        Assert.Null(actual.NullableInt);
    }
}
