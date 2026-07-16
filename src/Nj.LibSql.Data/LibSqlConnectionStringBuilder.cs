using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

namespace Nj.LibSql.Data;

/// <summary>
/// Provides a simple way to create and manage the contents of connection strings used by
/// <see cref="LibSqlConnection"/>.
/// </summary>
public sealed class LibSqlConnectionStringBuilder : DbConnectionStringBuilder
{
    private static readonly HashSet<string> ValidKeywords = new(StringComparer.OrdinalIgnoreCase)
    {
        "Data Source",
        "DataSource",
        "Database",
        "DB",
        "Uri",
        "Url",
        "Auth Token",
        "AuthToken",
        "Token",
        "Encryption Key",
        "EncryptionKey",
        "Key",
        "Sync URL",
        "SyncURL",
        "SyncUrl",
        "Sync Auth Token",
        "SyncAuthToken",
        "SyncToken",
    };

    private const string InMemoryConnectionString = ":memory:";
    private const string SharedMemoryConnectionString = ":memory:?cache=shared";

    private string? _dataSource;
    private string? _authToken;
    private string? _encryptionKey;
    private string? _syncUrl;
    private string? _syncAuthToken;
    private LibSqlConnectionMode _mode = LibSqlConnectionMode.Local;

    public LibSqlConnectionStringBuilder()
    {
    }

    public LibSqlConnectionStringBuilder(string connectionString)
    {
        ConnectionString = connectionString;
        RefreshFromBase();
    }

    /// <summary>Gets or sets the data source (file path, <c>:memory:</c>, or a remote URL).</summary>
    public string? DataSource
    {
        get => _dataSource;
        set
        {
            _dataSource = value;
            base["Data Source"] = value;
            UpdateMode();
        }
    }

    /// <summary>Gets or sets the authentication token for remote connections.</summary>
    public string? AuthToken
    {
        get => _authToken;
        set
        {
            _authToken = value;
            base["Auth Token"] = value;
        }
    }

    /// <summary>Gets or sets the encryption key for encrypted local databases.</summary>
    public string? EncryptionKey
    {
        get => _encryptionKey;
        set
        {
            _encryptionKey = value;
            base["Encryption Key"] = value;
        }
    }

    /// <summary>Gets or sets the sync URL for embedded replica connections.</summary>
    public string? SyncUrl
    {
        get => _syncUrl;
        set
        {
            _syncUrl = value;
            base["Sync URL"] = value;
            UpdateMode();
        }
    }

    /// <summary>Gets or sets the authentication token used for sync operations.</summary>
    public string? SyncAuthToken
    {
        get => _syncAuthToken;
        set
        {
            _syncAuthToken = value;
            base["Sync Auth Token"] = value;
        }
    }

    /// <summary>Gets the connection mode inferred from the current configuration.</summary>
    public LibSqlConnectionMode Mode => _mode;

    [AllowNull]
    public override object this[string keyword]
    {
        get => base[keyword];
        set
        {
            if (!IsValidKeyword(keyword))
            {
                throw new ArgumentException($"Keyword not supported: '{keyword}'", nameof(keyword));
            }

            var normalizedKeyword = NormalizeKeyword(keyword);
            switch (normalizedKeyword)
            {
                case "Data Source":
                    DataSource = value?.ToString();
                    break;
                case "Auth Token":
                    AuthToken = value?.ToString();
                    break;
                case "Encryption Key":
                    EncryptionKey = value?.ToString();
                    break;
                case "Sync URL":
                    SyncUrl = value?.ToString();
                    break;
                case "Sync Auth Token":
                    SyncAuthToken = value?.ToString();
                    break;
                default:
                    base[keyword] = value;
                    break;
            }
        }
    }

    /// <summary>Creates an in-memory connection string.</summary>
    public static string CreateInMemoryConnectionString()
        => $"Data Source={InMemoryConnectionString}";

    /// <summary>Creates a shared in-memory connection string.</summary>
    public static string CreateSharedMemoryConnectionString()
        => $"Data Source={SharedMemoryConnectionString}";

    private static bool IsValidKeyword(string keyword)
        => ValidKeywords.Contains(keyword);

    private static string NormalizeKeyword(string keyword)
        => keyword.ToLowerInvariant() switch
        {
            "datasource" or "database" or "db" or "uri" or "url" => "Data Source",
            "authtoken" or "token" => "Auth Token",
            "encryptionkey" or "key" => "Encryption Key",
            "syncurl" => "Sync URL",
            "syncauthtoken" or "synctoken" => "Sync Auth Token",
            _ => keyword
        };

    private void UpdateMode()
    {
        if (string.IsNullOrWhiteSpace(_dataSource))
        {
            _mode = LibSqlConnectionMode.Local;
        }
        else if (_dataSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                 || _dataSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
                 || _dataSource.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase)
                 || _dataSource.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
                 || _dataSource.StartsWith("ws://", StringComparison.OrdinalIgnoreCase))
        {
            _mode = !string.IsNullOrWhiteSpace(_syncUrl)
                ? LibSqlConnectionMode.EmbeddedReplica
                : LibSqlConnectionMode.Remote;
        }
        else
        {
            _mode = !string.IsNullOrWhiteSpace(_syncUrl)
                ? LibSqlConnectionMode.EmbeddedReplica
                : LibSqlConnectionMode.Local;
        }
    }

    public override void Clear()
    {
        base.Clear();
        _dataSource = null;
        _authToken = null;
        _encryptionKey = null;
        _syncUrl = null;
        _syncAuthToken = null;
        _mode = LibSqlConnectionMode.Local;
    }

    public override bool ContainsKey(string keyword)
        => IsValidKeyword(keyword);

    public override bool Remove(string keyword)
    {
        if (!IsValidKeyword(keyword))
        {
            return false;
        }

        var normalizedKeyword = NormalizeKeyword(keyword);
        var result = base.Remove(normalizedKeyword);

        if (result)
        {
            switch (normalizedKeyword)
            {
                case "Data Source":
                    _dataSource = null;
                    UpdateMode();
                    break;
                case "Auth Token":
                    _authToken = null;
                    break;
                case "Encryption Key":
                    _encryptionKey = null;
                    break;
                case "Sync URL":
                    _syncUrl = null;
                    UpdateMode();
                    break;
                case "Sync Auth Token":
                    _syncAuthToken = null;
                    break;
            }
        }

        return result;
    }

    public override bool TryGetValue(string keyword, [NotNullWhen(true)] out object? value)
    {
        if (!IsValidKeyword(keyword))
        {
            value = null;
            return false;
        }

        return base.TryGetValue(NormalizeKeyword(keyword), out value);
    }

    private void RefreshFromBase()
    {
        if (base.TryGetValue("Data Source", out var dataSource))
        {
            _dataSource = dataSource?.ToString();
        }

        if (base.TryGetValue("Auth Token", out var authToken))
        {
            _authToken = authToken?.ToString();
        }

        if (base.TryGetValue("Encryption Key", out var encryptionKey))
        {
            _encryptionKey = encryptionKey?.ToString();
        }

        if (base.TryGetValue("Sync URL", out var syncUrl))
        {
            _syncUrl = syncUrl?.ToString();
        }

        if (base.TryGetValue("Sync Auth Token", out var syncAuthToken))
        {
            _syncAuthToken = syncAuthToken?.ToString();
        }

        UpdateMode();
    }

    /// <summary>Returns a redacted copy of a connection string, masking auth token values.</summary>
    internal static string Redact(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return string.Empty;
        }

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            foreach (var sensitiveKey in new[] { "Auth Token", "AuthToken", "Token", "Sync Auth Token", "SyncAuthToken", "SyncToken", "Encryption Key", "EncryptionKey", "Key" })
            {
                if (builder.ContainsKey(sensitiveKey))
                {
                    builder[sensitiveKey] = "***REDACTED***";
                }
            }

            return builder.ConnectionString;
        }
        catch
        {
            return "***REDACTED***";
        }
    }
}

/// <summary>Specifies the connection mode for libSQL.</summary>
public enum LibSqlConnectionMode
{
    /// <summary>Local file-based or in-memory database connection.</summary>
    Local,

    /// <summary>Remote HTTP-based database connection (not implemented in Phase 1).</summary>
    Remote,

    /// <summary>Embedded replica with sync capabilities (not implemented in Phase 1).</summary>
    EmbeddedReplica
}
