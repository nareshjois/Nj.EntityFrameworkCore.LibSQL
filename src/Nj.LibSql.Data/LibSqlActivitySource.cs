using System.Diagnostics;

namespace Nj.LibSql.Data;

/// <summary>
/// Thin <see cref="ActivitySource"/> for remote command execute and embedded-replica Sync.
/// Subscribe with an <see cref="ActivityListener"/> or OpenTelemetry. No automatic retries.
/// </summary>
public static class LibSqlActivitySource
{
    /// <summary>Activity source name: <c>Nj.LibSql.Data</c>.</summary>
    public const string Name = "Nj.LibSql.Data";

    internal static readonly ActivitySource Source = new(Name);

    internal static Activity? StartRemoteCommand(string operationName, string? commandText)
    {
        var activity = Source.StartActivity(operationName, ActivityKind.Client);
        if (activity is null)
        {
            return null;
        }

        activity.SetTag("db.system", "libsql");
        activity.SetTag("db.operation", operationName);
        activity.SetTag("libsql.connection.mode", "remote");
        if (!string.IsNullOrEmpty(commandText))
        {
            // Truncate; never include connection strings / tokens here.
            var sql = commandText.Length <= 256 ? commandText : commandText[..256] + "…";
            activity.SetTag("db.statement", sql);
        }

        return activity;
    }

    internal static Activity? StartSync()
    {
        var activity = Source.StartActivity("libsql.sync", ActivityKind.Client);
        activity?.SetTag("db.system", "libsql");
        activity?.SetTag("db.operation", "sync");
        activity?.SetTag("libsql.connection.mode", "embedded_replica");
        return activity;
    }
}
