using Nj.LibSql.Bindings;

namespace Nj.LibSql.Data.Internal;

/// <summary>
/// Manages a cache of prepared statements for improved performance.
/// </summary>
internal sealed class LibSqlStatementCache : IDisposable
{
    private readonly LruCache<string, CachedStatement> _cache;
    private bool _disposed;

    public LibSqlStatementCache(int maxStatements = 100)
    {
        MaxStatements = maxStatements;
        _cache = new LruCache<string, CachedStatement>(maxStatements);
    }

    public int Count => _cache.Count;

    public int MaxStatements { get; }

    public bool TryGetStatement(string sql, out LibSqlStatementHandle? statement)
    {
        if (_cache.TryGetValue(sql, out var cached))
        {
            statement = cached.Statement;
            return true;
        }

        statement = null;
        return false;
    }

    public void AddStatement(string sql, LibSqlStatementHandle statement)
    {
        var cached = new CachedStatement { Statement = statement };
        _cache.AddOrUpdate(sql, cached);
    }

    public bool RemoveStatement(string sql)
        => _cache.Remove(sql);

    public void Clear()
        => _cache.Clear();

    public void Dispose()
    {
        if (_disposed)
        {
            return;
        }

        Clear();
        _disposed = true;
    }

    private sealed class CachedStatement : IDisposable
    {
        public LibSqlStatementHandle? Statement { get; set; }

        public void Dispose()
            => Statement?.Dispose();
    }
}
