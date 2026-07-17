using Microsoft.EntityFrameworkCore;
using Nj.EntityFrameworkCore.LibSql;
using Nj.LibSql.Data;

// EmbeddedReplicaSample — local file replica + Sync URL primary.
// Validated against self-hosted sqld. Turso Sync currently hangs (C-019).
//
//   export LIBSQL_SAMPLE_PRIMARY=http://127.0.0.1:8080
//   # optional: LIBSQL_SAMPLE_AUTH_TOKEN=…

var primary =
    Environment.GetEnvironmentVariable("LIBSQL_SAMPLE_PRIMARY")
    ?? "http://127.0.0.1:8080";
var token = Environment.GetEnvironmentVariable("LIBSQL_SAMPLE_AUTH_TOKEN");
var replicaPath = Path.Combine(Path.GetTempPath(), "nj-embedded-replica-sample.db");

var replicaCs = string.IsNullOrWhiteSpace(token)
    ? $"Data Source={replicaPath};Sync URL={primary};Read Your Writes=False"
    : $"Data Source={replicaPath};Sync URL={primary};Auth Token={token};Read Your Writes=False";

Console.WriteLine(LibSqlProviderInfo.GetScaffoldStatus());
Console.WriteLine($"Replica file: {replicaPath}");
Console.WriteLine($"Primary: {primary}");

// Seed primary over HTTP, then Sync into the local replica.
await using (var primaryContext = new SampleContext(
                   new DbContextOptionsBuilder<SampleContext>()
                       .UseLibSql($"Data Source={primary}" + (string.IsNullOrWhiteSpace(token) ? "" : $";Auth Token={token}"))
                       .Options))
{
    await primaryContext.Database.EnsureCreatedAsync();
    primaryContext.Notes.Add(new Note { Text = "from-primary-" + Guid.NewGuid().ToString("N")[..8] });
    await primaryContext.SaveChangesAsync();
}

await using var replica = new SampleContext(
    new DbContextOptionsBuilder<SampleContext>()
        .UseLibSql(replicaCs)
        .Options);

var sync = await replica.Database.SyncAsync();
Console.WriteLine($"Sync: {sync}");
Console.WriteLine($"Replica notes: {await replica.Notes.CountAsync()}");

try
{
    if (File.Exists(replicaPath))
    {
        LibSqlConnection.ClearPool(new LibSqlConnection(replicaCs));
        File.Delete(replicaPath);
    }
}
catch
{
    // best-effort cleanup
}

file sealed class SampleContext(DbContextOptions<SampleContext> options) : DbContext(options)
{
    public DbSet<Note> Notes => Set<Note>();
}

file sealed class Note
{
    public int Id { get; set; }
    public string Text { get; set; } = "";
}
