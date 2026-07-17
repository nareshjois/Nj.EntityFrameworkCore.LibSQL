using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql;
using Nj.LibSql.Data;

// RemoteSample — point UseLibSql at sqld or Turso via connection string only.
//
// Self-hosted:
//   Data Source=http://127.0.0.1:8080
// Turso:
//   Data Source=libsql://<db>-<org>.turso.io;Auth Token=<token>
//   (or https://… — Cloud rejects WebSocket upgrades)

var connectionString =
    Environment.GetEnvironmentVariable("LIBSQL_SAMPLE_CONNECTION")
    ?? "Data Source=http://127.0.0.1:8080";

Console.WriteLine(LibSqlProviderInfo.GetScaffoldStatus());
Console.WriteLine($"RemoteSample connection: {LibSqlConnectionStringBuilder.Redact(connectionString)}");

await using var context = new SampleContext(
    new DbContextOptionsBuilder<SampleContext>()
        .UseLibSql(connectionString)
        .Options);

await context.Database.EnsureCreatedAsync();
context.Notes.Add(new Note { Text = "hello from RemoteSample" });
await context.SaveChangesAsync();
Console.WriteLine($"Notes: {await context.Notes.CountAsync()}");

file sealed class SampleContext(DbContextOptions<SampleContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

file sealed class Note
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
}
