namespace Nj.LibSql.Data;

/// <summary>
/// Specifies the transaction behavior for a libSQL transaction. These behaviors control how
/// locks are acquired, not SQL isolation levels.
/// </summary>
public enum LibSqlTransactionBehavior
{
    /// <summary>Locks are not acquired until the first database access. This is the default.</summary>
    Deferred,

    /// <summary>Acquires a reserved lock immediately to prevent deadlocks.</summary>
    Immediate,

    /// <summary>Acquires an exclusive lock immediately.</summary>
    Exclusive,

    /// <summary>libSQL extension for read-only transactions; cannot perform write operations.</summary>
    ReadOnly
}
