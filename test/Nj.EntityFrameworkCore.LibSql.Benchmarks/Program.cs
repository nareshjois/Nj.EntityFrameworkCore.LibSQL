using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using BenchmarkDotNet.Running;
using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql;
using Nj.LibSql.Data;

namespace Nj.EntityFrameworkCore.LibSql.Benchmarks;

public static class Program
{
    public static int Main(string[] args)
    {
        // UnrollFactor=1 avoids native-handle exhaustion (SQLITE_CANTOPEN / error 14)
        // when BenchmarkDotNet would otherwise open thousands of connections per iteration.
        var config = DefaultConfig.Instance
            .AddJob(Job.ShortRun
                .WithWarmupCount(1)
                .WithIterationCount(5)
                .WithInvocationCount(64)
                .WithUnrollFactor(1))
            .WithOptions(ConfigOptions.DisableOptimizationsValidator);

        _ = BenchmarkRunner.Run<LocalProviderBenchmarks>(config, args);
        return 0;
    }
}

/// <summary>
/// Local-mode baselines for WP-12 Preview. Soft threshold: investigate if a
/// scenario regresses by more than ~2× on the same runner class vs prior artifact.
/// </summary>
[MemoryDiagnoser]
public class LocalProviderBenchmarks
{
    private string _workDir = "";
    private string _path = "";
    private string _sqlitePath = "";
    private LibSqlConnection? _queryConnection;
    private int _coldOpenCounter;

    [GlobalSetup]
    public void Setup()
    {
        _workDir = Path.Combine(Path.GetTempPath(), "nj-bench-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(_workDir);
        _path = Path.Combine(_workDir, "libsql.db");
        _sqlitePath = Path.Combine(_workDir, "sqlite.db");

        using (var ctx = CreateLibSql())
        {
            ctx.Database.EnsureCreated();
        }

        using (var sqlite = CreateSqlite())
        {
            sqlite.Database.EnsureCreated();
        }
    }

    [GlobalSetup(Target = nameof(LibSql_SelectOne))]
    public void SetupSelectOne()
    {
        Setup();
        _queryConnection = new LibSqlConnection($"Data Source={_path}");
        _queryConnection.Open();
    }

    [GlobalCleanup]
    public void Cleanup()
    {
        try
        {
            _queryConnection?.Dispose();
            _queryConnection = null;
            LibSqlConnection.ClearAllPools();

            if (!string.IsNullOrEmpty(_workDir) && Directory.Exists(_workDir))
            {
                Directory.Delete(_workDir, recursive: true);
            }
        }
        catch
        {
            // best-effort
        }
    }

    [Benchmark(Description = "LibSql cold Open")]
    public void LibSql_ColdOpen()
    {
        // Unique file per invoke avoids leftover native locks under tight BDN loops.
        var path = Path.Combine(_workDir, "cold-" + Interlocked.Increment(ref _coldOpenCounter) + ".db");
        using var connection = new LibSqlConnection($"Data Source={path}");
        connection.Open();
    }

    [Benchmark(Description = "LibSql SELECT 1")]
    public long LibSql_SelectOne()
    {
        using var command = _queryConnection!.CreateCommand();
        command.CommandText = "SELECT 1";
        return Convert.ToInt64(command.ExecuteScalar());
    }

    [Benchmark(Description = "LibSql EF insert batch 50")]
    public void LibSql_EfInsertBatch50()
    {
        using var context = CreateLibSql();
        for (var i = 0; i < 50; i++)
        {
            context.Items.Add(new BenchItem { Name = "b" + i });
        }

        context.SaveChanges();
        context.ChangeTracker.Clear();
    }

    [Benchmark(Description = "LibSql EF short transaction")]
    public void LibSql_EfShortTransaction()
    {
        using var context = CreateLibSql();
        using var tx = context.Database.BeginTransaction();
        context.Items.Add(new BenchItem { Name = "tx" });
        context.SaveChanges();
        tx.Commit();
        context.ChangeTracker.Clear();
    }

    [Benchmark(Description = "Sqlite EF insert batch 50 (differential)")]
    public void Sqlite_EfInsertBatch50()
    {
        using var context = CreateSqlite();
        for (var i = 0; i < 50; i++)
        {
            context.Items.Add(new BenchItem { Name = "s" + i });
        }

        context.SaveChanges();
        context.ChangeTracker.Clear();
    }

    private BenchContext CreateLibSql()
        => new(new DbContextOptionsBuilder<BenchContext>()
            .UseLibSql($"Data Source={_path}")
            .Options);

    private BenchContext CreateSqlite()
        => new(new DbContextOptionsBuilder<BenchContext>()
            .UseSqlite($"Data Source={_sqlitePath}")
            .Options);

    private sealed class BenchContext(DbContextOptions<BenchContext> options) : DbContext(options)
    {
        public DbSet<BenchItem> Items => Set<BenchItem>();
    }

    private sealed class BenchItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = "";
    }
}
