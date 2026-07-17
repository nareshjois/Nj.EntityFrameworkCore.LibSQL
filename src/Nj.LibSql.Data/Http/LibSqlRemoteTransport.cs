namespace Nj.LibSql.Data.Http;

/// <summary>
/// Remote URL routing (aligned with <c>@libsql/client</c> <c>preferHttp: true</c>):
/// <list type="bullet">
/// <item><c>http(s)://</c> and <c>libsql://</c> → HTTP Hrana</item>
/// <item><c>ws(s)://</c> → WebSocket Hrana</item>
/// </list>
/// Turso Cloud rejects WebSocket upgrades; self-hosted <c>sqld</c> accepts them on <c>/</c>.
/// </summary>
internal static class LibSqlRemoteTransport
{
    /// <summary>IsWebSocketUrl(string.</summary>
    public static bool IsWebSocketUrl(string url)
        => url.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
           || url.StartsWith("ws://", StringComparison.OrdinalIgnoreCase);

    /// <summary>IsHttpUrl(string.</summary>
    public static bool IsHttpUrl(string url)
        => url.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
           || url.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
           || url.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase);

    /// <summary>Maps <c>libsql://</c> to <c>https://</c> for the HTTP transport.</summary>
    public static string NormalizeLibSqlToHttpUrl(string url)
    {
        if (url.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase))
        {
            return string.Concat("https://", url.AsSpan("libsql://".Length));
        }

        return url;
    }
}
