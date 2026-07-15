namespace Nj.EntityFrameworkCore.LibSql.TestUtilities;

/// <summary>
/// Shared helpers for local / remote / replica connection strings and env flags.
/// </summary>
public static class TestEnvironment
{
    public const string RemoteUrlEnvironmentVariable = "LIBSQL_TEST_URL";
    public const string DisableRemoteEnvironmentVariable = "LIBSQL_DISABLE_REMOTE_TESTS";
    public const string DisableTestcontainersEnvironmentVariable = "LIBSQL_DISABLE_TESTCONTAINERS";

    /// <summary>
    /// Pinned self-hosted sqld image (digest). Keep in sync with
    /// <c>eng/sqld/docker-compose.yml</c> and <c>docs/versions.md</c>.
    /// </summary>
    public const string SqldImage =
        "ghcr.io/tursodatabase/libsql-server:ef758d9@sha256:817fb6c6865d048a509f5c120905629fb9b5af20ad0c526cdc68a6d8793898ad";

    /// <summary>Default local file mode connection string for smoke tests.</summary>
    public static string LocalConnectionString(string databasePath)
        => $"Data Source={databasePath}";

    /// <summary>In-memory local connection string.</summary>
    public static string InMemoryConnectionString
        => "Data Source=:memory:";

    /// <summary>
    /// Optional external remote URL / connection string. When set, Testcontainers
    /// is not started and this endpoint is used instead.
    /// </summary>
    public static string? ExternalRemoteSqldUrl
        => Environment.GetEnvironmentVariable(RemoteUrlEnvironmentVariable);

    /// <summary>
    /// Remote tests run unless explicitly disabled.
    /// </summary>
    public static bool RemoteTestsDisabled
        => string.Equals(
            Environment.GetEnvironmentVariable(DisableRemoteEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    /// <summary>
    /// When true, do not start a Testcontainers sqld; only use
    /// <see cref="ExternalRemoteSqldUrl"/> (or skip).
    /// </summary>
    public static bool TestcontainersDisabled
        => string.Equals(
            Environment.GetEnvironmentVariable(DisableTestcontainersEnvironmentVariable),
            "1",
            StringComparison.Ordinal);

    /// <summary>Build a Nelknet connection string for a remote HTTP(S) endpoint.</summary>
    public static string RemoteConnectionStringFromUrl(string urlOrConnectionString)
    {
        var raw = urlOrConnectionString.Trim();
        if (raw.Contains('=', StringComparison.Ordinal)
            && raw.Contains("Data Source", StringComparison.OrdinalIgnoreCase))
        {
            return raw;
        }

        return $"Data Source={raw}";
    }
}
