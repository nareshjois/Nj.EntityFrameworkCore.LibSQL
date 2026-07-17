using Nj.LibSql.Data;

var path = Path.Combine(Path.GetTempPath(), $"nj-libsql-aot-{Guid.NewGuid():N}.db");
await using var connection = new LibSqlConnection($"Data Source={path}");
await connection.OpenAsync();
await using var command = connection.CreateCommand();
command.CommandText = "SELECT 1";
Console.WriteLine("SELECT 1 => " + await command.ExecuteScalarAsync());
