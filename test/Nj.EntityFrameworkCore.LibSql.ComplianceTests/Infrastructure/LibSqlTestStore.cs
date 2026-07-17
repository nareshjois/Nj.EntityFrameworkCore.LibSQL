using System.Collections.Concurrent;
using System.Data.Common;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Infrastructure;
using Microsoft.EntityFrameworkCore.Storage;
using Microsoft.Extensions.DependencyInjection;
using Nj.LibSql.Data;

namespace Nj.EntityFrameworkCore.LibSql.ComplianceTests.Infrastructure;

public class LibSqlTestStore : RelationalTestStore
{
    public const int CommandTimeout = 30;

    private static readonly ConcurrentDictionary<string, LibSqlTestStore> SharedStores = new(StringComparer.OrdinalIgnoreCase);

    private readonly bool _seed;

    public static LibSqlTestStore Create(string name)
        => new(name, seed: true, shared: false, sharedCache: false, uniquePath: true);

    public static LibSqlTestStore GetOrCreate(string name, bool sharedCache = false)
    {
        var key = name + (sharedCache ? "|shared" : "");
        return SharedStores.GetOrAdd(
            key,
            _ => new LibSqlTestStore(name, seed: true, shared: true, sharedCache: sharedCache, uniquePath: false));
    }

    private LibSqlTestStore(string name, bool seed, bool shared, bool sharedCache, bool uniquePath)
        : base(name, shared, CreateConnection(name, sharedCache, uniquePath))
        => _seed = seed;

    public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
        => UseConnectionString
            ? builder.UseLibSql(
                ConnectionString,
                b =>
                {
                    b.CommandTimeout(CommandTimeout);
                    b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                })
            : builder.UseLibSql(
                Connection,
                b =>
                {
                    b.CommandTimeout(CommandTimeout);
                    b.UseQuerySplittingBehavior(QuerySplittingBehavior.SingleQuery);
                });

    protected override async Task InitializeAsync(
        Func<DbContext> createContext,
        Func<DbContext, Task>? seed,
        Func<DbContext, Task>? clean)
    {
        if (!_seed)
        {
            return;
        }

        await using var context = createContext();
        var databaseCreator = context.GetService<IRelationalDatabaseCreator>();
        if (await databaseCreator.ExistsAsync())
        {
            if (clean is not null)
            {
                await clean(context);
            }

            await CleanAsync(context);
        }

        await context.Database.EnsureCreatedResilientlyAsync();
        if (seed is not null)
        {
            await seed(context);
        }
    }

    public override Task CleanAsync(DbContext context)
    {
        context.Database.EnsureClean();
        return Task.CompletedTask;
    }

    private static LibSqlConnection CreateConnection(string name, bool sharedCache, bool uniquePath)
    {
        if (string.Equals(name, ":memory:", StringComparison.OrdinalIgnoreCase))
        {
            var connectionString = sharedCache
                ? LibSqlConnectionStringBuilder.CreateSharedMemoryConnectionString()
                : LibSqlConnectionStringBuilder.CreateInMemoryConnectionString();
            return new LibSqlConnection(connectionString);
        }

        var fileName = uniquePath ? name + "-" + Guid.NewGuid().ToString("N") + ".db" : name + ".db";
        var databasePath = Path.Combine(Path.GetTempPath(), "nj-libsql-spec", fileName);
        Directory.CreateDirectory(Path.GetDirectoryName(databasePath)!);
        return new LibSqlConnection($"Data Source={databasePath}");
    }
}

public class LibSqlTestStoreFactory : RelationalTestStoreFactory
{
    public static LibSqlTestStoreFactory Instance { get; } = new();

    protected LibSqlTestStoreFactory()
    {
    }

    public override TestStore Create(string storeName)
        => LibSqlTestStore.Create(storeName);

    public override TestStore GetOrCreate(string storeName)
        => LibSqlTestStore.GetOrCreate(storeName);

    public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkLibSql();
}

public sealed class SharedCacheLibSqlTestStoreFactory : LibSqlTestStoreFactory
{
    public static new SharedCacheLibSqlTestStoreFactory Instance { get; } = new();

    // Named for EF Sqlite parity. libSQL does not honor SQLite shared-cache URI/mode,
    // so DirtyReadsOccur is false in TransactionLibSqlTest; this still uses a shared file store.
    public override TestStore GetOrCreate(string storeName)
        => LibSqlTestStore.GetOrCreate(storeName, sharedCache: false);
}

public sealed class RemoteLibSqlTestStoreFactory : RelationalTestStoreFactory
{
    private readonly string _connectionString;

    public RemoteLibSqlTestStoreFactory(string connectionString)
        => _connectionString = connectionString;

    public override TestStore Create(string storeName)
        => new RemoteLibSqlTestStore(storeName, _connectionString);

    public override TestStore GetOrCreate(string storeName)
        => Create(storeName);

    public override IServiceCollection AddProviderServices(IServiceCollection serviceCollection)
        => serviceCollection.AddEntityFrameworkLibSql();
}

internal sealed class RemoteLibSqlTestStore : RelationalTestStore
{
    public RemoteLibSqlTestStore(string name, string connectionString)
        : base(name, shared: true, new LibSqlConnection(connectionString))
    {
        UseConnectionString = true;
    }

    public override DbContextOptionsBuilder AddProviderOptions(DbContextOptionsBuilder builder)
        => builder.UseLibSql(Connection.ConnectionString ?? ConnectionString);

    protected override async Task InitializeAsync(
        Func<DbContext> createContext,
        Func<DbContext, Task>? seed,
        Func<DbContext, Task>? clean)
    {
        await using var context = createContext();
        await context.Database.EnsureCreatedResilientlyAsync();
        if (seed is not null)
        {
            await seed(context);
        }
    }

    public override Task CleanAsync(DbContext context)
    {
        context.Database.EnsureClean();
        return Task.CompletedTask;
    }
}
