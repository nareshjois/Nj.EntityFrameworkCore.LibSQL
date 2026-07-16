using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Nj.EntityFrameworkCore.LibSql;

namespace MigrationsSample;

/// <summary>
/// Design-time factory for <c>dotnet ef</c>. Connection comes from
/// <c>LIBSQL_CONNECTION</c>, or defaults to a local file under the sample directory.
/// </summary>
public sealed class SampleDbContextFactory : IDesignTimeDbContextFactory<SampleDbContext>
{
    public SampleDbContext CreateDbContext(string[] args)
    {
        var connectionString = ResolveConnectionString();
        var options = new DbContextOptionsBuilder<SampleDbContext>()
            .UseLibSql(connectionString)
            .Options;
        return new SampleDbContext(options);
    }

    internal static string ResolveConnectionString()
    {
        var fromEnv = Environment.GetEnvironmentVariable("LIBSQL_CONNECTION");
        if (!string.IsNullOrWhiteSpace(fromEnv))
        {
            return fromEnv;
        }

        var path = Path.GetFullPath(
            Path.Combine(Directory.GetCurrentDirectory(), "migrations-sample.db"));
        return $"Data Source={path}";
    }
}
