using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql;

var path = Path.Combine(Path.GetTempPath(), "nj-libsql-local-sample.db");
await using var context = new SampleContext(
    new DbContextOptionsBuilder<SampleContext>()
        .UseLibSql($"Data Source={path}")
        .Options);

Console.WriteLine(LibSqlProviderInfo.GetScaffoldStatus());
await context.Database.OpenConnectionAsync();
await using (var command = context.Database.GetDbConnection().CreateCommand())
{
    command.CommandText = "SELECT 1";
    Console.WriteLine("SELECT 1 => " + await command.ExecuteScalarAsync());
}

await context.Database.CloseConnectionAsync();

file sealed class SampleContext(DbContextOptions<SampleContext> options) : DbContext(options);
