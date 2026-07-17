using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;

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
        "Filename",
        "File Name",
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
        "Sync Interval",
        "SyncInterval",
        "Read Your Writes",
        "ReadYourWrites",
        "Offline",
        "Tls",
        // Microsoft.Data.Sqlite compat — accepted and ignored (soft migration).
        "Mode",
        "Cache",
        "Foreign Keys",
        "ForeignKeys",
        "Recursive Triggers",
        "RecursiveTriggers",
        "Pooling",
        "Vfs",
        "Default Timeout",
        "DefaultTimeout",
        "Command Timeout",
        "CommandTimeout",
    };

    private const string InMemoryConnectionString = ":memory:";
    private const string SharedMemoryConnectionString = ":memory:?cache=shared";

    private string? _dataSource;
    private string? _authToken;
    private string? _encryptionKey;
    private string? _syncUrl;
    private string? _syncAuthToken;
    private int _syncInterval;
    private bool _readYourWrites = true;
    private bool _offline;
    private bool _tls = true;
    private LibSqlConnectionMode _mode = LibSqlConnectionMode.Local;

    /// <summary>Initializes a new instance of the <see cref="LibSqlConnectionStringBuilder"/> class.</summary>
    public LibSqlConnectionStringBuilder()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConnectionStringBuilder"/> class.</summary>
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

    /// <summary>
    /// Gets or sets automatic sync interval in seconds (0 = manual <see cref="LibSqlConnection.Sync"/> only).
    /// Matches the native libSQL <c>sync_interval</c> field.
    /// </summary>
    public int SyncInterval
    {
        get => _syncInterval;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _syncInterval = value;
            base["Sync Interval"] = value;
        }
    }

    /// <summary>
    /// When <see langword="true"/> (default), local writes are visible before the next remote sync.
    /// </summary>
    public bool ReadYourWrites
    {
        get => _readYourWrites;
        set
        {
            _readYourWrites = value;
            base["Read Your Writes"] = value;
        }
    }

    /// <summary>
    /// When <see langword="true"/>, appends <c>?offline</c> to the sync URL so the native
    /// client opens without contacting the primary until <see cref="LibSqlConnection.Sync"/>.
    /// (Pinned natives do not expose a separate offline config field.)
    /// </summary>
    public bool Offline
    {
        get => _offline;
        set
        {
            _offline = value;
            base["Offline"] = value;
        }
    }

    /// <summary>
    /// When <see langword="true"/> (default), <c>libsql://</c> remotes use HTTPS.
    /// Set <see langword="false"/> to map <c>libsql://</c> to HTTP (local sqld).
    /// </summary>
    public bool Tls
    {
        get => _tls;
        set
        {
            _tls = value;
            base["Tls"] = value;
        }
    }

    /// <summary>Gets the connection mode inferred from the current configuration.</summary>
    public LibSqlConnectionMode Mode => _mode;

    /// <summary>
    /// Auth token used for embedded-replica sync: <see cref="SyncAuthToken"/> if set,
    /// otherwise <see cref="AuthToken"/>.
    /// </summary>
    public string? EffectiveSyncAuthToken
        => !string.IsNullOrEmpty(_syncAuthToken) ? _syncAuthToken : _authToken;

    /// <inheritdoc />
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
                case "Sync Interval":
                    SyncInterval = ConvertToInt32(value);
                    break;
                case "Read Your Writes":
                    ReadYourWrites = ConvertToBoolean(value);
                    break;
                case "Offline":
                    Offline = ConvertToBoolean(value);
                    break;
                case "Tls":
                    Tls = ConvertToBoolean(value);
                    break;
                case "Mode":
                case "Cache":
                case "Foreign Keys":
                case "Recursive Triggers":
                case "Pooling":
                case "Vfs":
                case "Default Timeout":
                    // Accepted for Microsoft.Data.Sqlite connection-string soft migration; ignored.
                    base[normalizedKeyword] = value;
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
            "datasource" or "database" or "db" or "uri" or "url" or "filename" or "file name"
                => "Data Source",
            "authtoken" or "token" => "Auth Token",
            "encryptionkey" or "key" => "Encryption Key",
            "syncurl" => "Sync URL",
            "syncauthtoken" or "synctoken" => "Sync Auth Token",
            "syncinterval" => "Sync Interval",
            "readyourwrites" => "Read Your Writes",
            "offline" => "Offline",
            "tls" => "Tls",
            "foreignkeys" => "Foreign Keys",
            "recursivetriggers" => "Recursive Triggers",
            "defaulttimeout" or "commandtimeout" or "command timeout" => "Default Timeout",
            "mode" => "Mode",
            "cache" => "Cache",
            "pooling" => "Pooling",
            "vfs" => "Vfs",
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

    /// <inheritdoc />
    public override void Clear()
    {
        base.Clear();
        _dataSource = null;
        _authToken = null;
        _encryptionKey = null;
        _syncUrl = null;
        _syncAuthToken = null;
        _syncInterval = 0;
        _readYourWrites = true;
        _offline = false;
        _tls = true;
        _mode = LibSqlConnectionMode.Local;
    }

    /// <inheritdoc />
    public override bool ContainsKey(string keyword)
        => IsValidKeyword(keyword);

    /// <inheritdoc />
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
                case "Sync Interval":
                    _syncInterval = 0;
                    break;
                case "Read Your Writes":
                    _readYourWrites = true;
                    break;
                case "Offline":
                    _offline = false;
                    break;
                case "Tls":
                    _tls = true;
                    break;
            }
        }

        return result;
    }

    /// <inheritdoc />
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

        if (base.TryGetValue("Sync Interval", out var syncInterval))
        {
            _syncInterval = ConvertToInt32(syncInterval);
        }

        if (base.TryGetValue("Read Your Writes", out var readYourWrites))
        {
            _readYourWrites = ConvertToBoolean(readYourWrites);
        }

        if (base.TryGetValue("Offline", out var offline))
        {
            _offline = ConvertToBoolean(offline);
        }

        if (base.TryGetValue("Tls", out var tls))
        {
            _tls = ConvertToBoolean(tls);
        }

        UpdateMode();
    }

    private static int ConvertToInt32(object? value)
        => value switch
        {
            null => 0,
            int i => i,
            long l => checked((int)l),
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var parsed)
                => parsed,
            _ => Convert.ToInt32(value, CultureInfo.InvariantCulture)
        };

    private static bool ConvertToBoolean(object? value)
        => value switch
        {
            null => false,
            bool b => b,
            string s when bool.TryParse(s, out var parsed) => parsed,
            string s when int.TryParse(s, NumberStyles.Integer, CultureInfo.InvariantCulture, out var n)
                => n != 0,
            _ => Convert.ToBoolean(value, CultureInfo.InvariantCulture)
        };

    /// <summary>
    /// Returns a redacted copy of a connection string, masking auth tokens and encryption keys.
    /// Use this for logs, exceptions, and diagnostics — never echo raw connection strings.
    /// </summary>
    public static string Redact(string? connectionString)
    {
        if (string.IsNullOrEmpty(connectionString))
        {
            return string.Empty;
        }

        try
        {
            var builder = new DbConnectionStringBuilder { ConnectionString = connectionString };
            foreach (var sensitiveKey in new[]
                     {
                         "Auth Token", "AuthToken", "Token",
                         "Sync Auth Token", "SyncAuthToken", "SyncToken",
                         "Encryption Key", "EncryptionKey", "Key"
                     })
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

    /// <summary>Remote HTTP/WebSocket Hrana connection.</summary>
    Remote,

    /// <summary>Embedded replica with sync against a remote primary.</summary>
    EmbeddedReplica
}
