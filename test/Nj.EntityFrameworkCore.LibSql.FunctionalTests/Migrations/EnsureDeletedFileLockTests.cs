using Nelknet.LibSQL.Data;
using Xunit;

namespace Nj.EntityFrameworkCore.LibSql.FunctionalTests.Migrations;

/// <summary>
/// Regression for C-005: native close must release the Windows file lock.
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

            LibSQLConnection.ClearAllPools();
            Assert.True(File.Exists(path));
            File.Delete(path);
            Assert.False(File.Exists(path));
        }
        finally
        {
            try
            {
                if (File.Exists(path))
                {
                    LibSQLConnection.ClearAllPools();
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
