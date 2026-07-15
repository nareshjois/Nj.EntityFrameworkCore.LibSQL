namespace Nj.EntityFrameworkCore.LibSql.TestUtilities;

/// <summary>
/// Shared test helpers (connection-string builders, sqld readiness, etc.).
/// </summary>
public static class TestEnvironment
{
    /// <summary>Default local file mode connection string for smoke tests.</summary>
    public static string LocalConnectionString(string databasePath)
        => $"Data Source={databasePath}";

    /// <summary>Default remote sqld connection string (CI / docker compose).</summary>
    public static string RemoteSqldConnectionString
        => Environment.GetEnvironmentVariable("LIBSQL_TEST_URL")
           ?? "http://127.0.0.1:8080";
}
