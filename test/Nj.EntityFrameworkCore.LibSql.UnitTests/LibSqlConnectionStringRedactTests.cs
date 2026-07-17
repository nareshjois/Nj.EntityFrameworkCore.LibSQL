using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.UnitTests;

public class LibSqlConnectionStringRedactTests
{
    [Theory]
    [InlineData("Auth Token")]
    [InlineData("AuthToken")]
    [InlineData("Token")]
    public void Redact_masks_auth_token_aliases(string key)
    {
        var raw = $"Data Source=https://example.turso.io;{key}=super-secret-token-value";
        var redacted = LibSqlConnectionStringBuilder.Redact(raw);
        Assert.DoesNotContain("super-secret-token-value", redacted, StringComparison.Ordinal);
        Assert.Contains("***REDACTED***", redacted, StringComparison.Ordinal);
        Assert.Contains("example.turso.io", redacted, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Sync Auth Token")]
    [InlineData("SyncAuthToken")]
    [InlineData("SyncToken")]
    public void Redact_masks_sync_auth_token_aliases(string key)
    {
        var raw = $"Data Source=file:replica.db;Mode=EmbeddedReplica;Sync URL=http://127.0.0.1:8080;{key}=sync-secret-token";
        var redacted = LibSqlConnectionStringBuilder.Redact(raw);
        Assert.DoesNotContain("sync-secret-token", redacted, StringComparison.Ordinal);
        Assert.Contains("***REDACTED***", redacted, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("Encryption Key")]
    [InlineData("EncryptionKey")]
    [InlineData("Key")]
    public void Redact_masks_encryption_key_aliases(string key)
    {
        var raw = $"Data Source=secret.db;{key}=encryption-secret";
        var redacted = LibSqlConnectionStringBuilder.Redact(raw);
        Assert.DoesNotContain("encryption-secret", redacted, StringComparison.Ordinal);
        Assert.Contains("***REDACTED***", redacted, StringComparison.Ordinal);
    }

    [Fact]
    public void Redact_empty_returns_empty()
    {
        Assert.Equal(string.Empty, LibSqlConnectionStringBuilder.Redact(null));
        Assert.Equal(string.Empty, LibSqlConnectionStringBuilder.Redact(""));
    }
}
