using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql.Infrastructure.Internal;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.UnitTests;

public sealed class LibSqlSyncGuardTests
{
    [Fact]
    public void IsEmbeddedReplica_detects_sync_url()
    {
        Assert.True(
            LibSqlConnectionStringHelpers.IsEmbeddedReplica(
                "Data Source=/tmp/replica.db;Sync URL=http://127.0.0.1:8080"));
        Assert.False(
            LibSqlConnectionStringHelpers.IsEmbeddedReplica("Data Source=/tmp/local.db"));
        Assert.False(
            LibSqlConnectionStringHelpers.IsEmbeddedReplica("Data Source=http://127.0.0.1:8080"));
    }

    [Fact]
    public void IsRemote_false_for_embedded_replica_local_path()
    {
        Assert.False(
            LibSqlConnectionStringHelpers.IsRemote(
                "Data Source=/tmp/replica.db;Sync URL=http://127.0.0.1:8080"));
    }

    [Fact]
    public void TryGetLocalFilePath_null_for_embedded_replica()
    {
        Assert.Null(
            LibSqlConnectionStringHelpers.TryGetLocalFilePath(
                "Data Source=/tmp/replica.db;Sync URL=http://127.0.0.1:8080"));
    }

    [Fact]
    public void Sync_on_non_replica_connection_throws()
    {
        var options = new DbContextOptionsBuilder()
            .UseLibSql("Data Source=:memory:")
            .Options;

        using var context = new SyncGuardContext(options);
        var ex = Assert.Throws<InvalidOperationException>(() => context.Database.Sync());
        Assert.Contains("embedded-replica", ex.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public void ConnectionStringBuilder_effective_sync_token_falls_back_to_auth_token()
    {
        var builder = new LibSqlConnectionStringBuilder(
            "Data Source=/tmp/r.db;Sync URL=http://127.0.0.1:1;Auth Token=abc");
        Assert.Equal("abc", builder.EffectiveSyncAuthToken);

        builder.SyncAuthToken = "sync-only";
        Assert.Equal("sync-only", builder.EffectiveSyncAuthToken);
    }

    private sealed class SyncGuardContext(DbContextOptions options) : DbContext(options);
}
