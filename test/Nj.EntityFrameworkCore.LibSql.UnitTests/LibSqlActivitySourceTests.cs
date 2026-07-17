using System.Diagnostics;
using Nj.LibSql.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.UnitTests;

public class LibSqlActivitySourceTests
{
    [Fact]
    public void Remote_command_activity_is_produced_for_listeners()
    {
        var started = new List<string>();
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibSqlActivitySource.Name,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _)
                => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => started.Add(activity.OperationName),
        };
        ActivitySource.AddActivityListener(listener);

        using (var activity = LibSqlActivitySource.StartRemoteCommand("ExecuteNonQuery", "SELECT 1"))
        {
            Assert.NotNull(activity);
            Assert.Equal("libsql", activity.GetTagItem("db.system"));
            Assert.Equal("remote", activity.GetTagItem("libsql.connection.mode"));
            Assert.Equal("SELECT 1", activity.GetTagItem("db.statement"));
        }

        Assert.Contains("ExecuteNonQuery", started);
    }

    [Fact]
    public void Sync_activity_is_produced_for_listeners()
    {
        Activity? observed = null;
        using var listener = new ActivityListener
        {
            ShouldListenTo = source => source.Name == LibSqlActivitySource.Name,
            Sample = static (ref ActivityCreationOptions<ActivityContext> _)
                => ActivitySamplingResult.AllDataAndRecorded,
            ActivityStarted = activity => observed = activity,
        };
        ActivitySource.AddActivityListener(listener);

        using (var activity = LibSqlActivitySource.StartSync())
        {
            Assert.NotNull(activity);
            Assert.Equal("embedded_replica", activity.GetTagItem("libsql.connection.mode"));
        }

        Assert.NotNull(observed);
        Assert.Equal("libsql.sync", observed!.OperationName);
    }
}
