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

    /// <summary>
    /// Maps <c>libsql://</c> to HTTP(S) for the HTTP transport.
    /// When <paramref name="tls"/> is <see langword="false"/>, uses <c>http://</c>
    /// (local/dev sqld); otherwise <c>https://</c> (Turso Cloud default).
    /// </summary>
    public static string NormalizeLibSqlToHttpUrl(string url, bool tls = true)
    {
        if (url.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase))
        {
            var scheme = tls ? "https://" : "http://";
            return string.Concat(scheme, url.AsSpan("libsql://".Length));
        }

        return url;
    }
}
