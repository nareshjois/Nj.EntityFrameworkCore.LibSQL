using Nelknet.LibSQL.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

/// <summary>
/// Regression for C-005: native close must release (or relocate) the Windows file lock.
/// </summary>
public sealed class EnsureDeletedFileLockTests
{
    [Fact]
    public void LibSQLConnection_Close_releases_local_file_for_delete()
    {
        var path = Path.Combine(Path.GetTempPath(), "nj-libsql-close-" + Guid.NewGuid().ToString("N") + ".db");
        var cs = $"Data Source={path}";
        try
        {
            using (var connection = new LibSQLConnection(cs))
            {
                connection.Open();
                using (var create = connection.CreateCommand())
                {
                    create.CommandText = "CREATE TABLE t(id INTEGER PRIMARY KEY)";
                    create.ExecuteNonQuery();
                }

                using (var insert = connection.CreateCommand())
                {
                    insert.CommandText = "INSERT INTO t VALUES (1)";
                    insert.ExecuteNonQuery();
                }

                connection.Close();
            }

            GC.Collect();
            GC.WaitForPendingFinalizers();
            Assert.True(File.Exists(path));

            try
            {
                File.Delete(path);
            }
            catch (IOException) when (OperatingSystem.IsWindows())
            {
                // Mirror LibSqlDatabaseCreator: Move when Delete is blocked.
                var trash = Path.Combine(
                    Path.GetTempPath(),
                    "nj-libsql-trash",
                    Guid.NewGuid().ToString("N") + ".db");
                Directory.CreateDirectory(Path.GetDirectoryName(trash)!);
                File.Move(path, trash);
            }

            Assert.False(File.Exists(path));
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                {
                    File.Delete(path);
                }
            }
            catch
            {
                // best-effort cleanup
            }
        }
    }
}
