namespace Nj.LibSql.Data.Exceptions;

/// <summary>Represents errors that occur when the database is busy or locked.</summary>
public class LibSqlBusyException : LibSqlException
{
    /// <summary>Gets the type of lock that caused the busy condition.</summary>
    public LockType LockType { get; }

    /// <summary>Gets the timeout value that was exceeded, if applicable.</summary>
    public TimeSpan? Timeout { get; }

    /// <summary>Gets whether this is a database-level lock.</summary>
    public bool IsDatabaseLocked { get; }

    public LibSqlBusyException()
    {
    }

    public LibSqlBusyException(string message)
        : base(message)
    {
    }

    public LibSqlBusyException(string message, LockType lockType, bool isDatabaseLocked = false)
        : base(message)
    {
        LockType = lockType;
        IsDatabaseLocked = isDatabaseLocked;
    }

    public LibSqlBusyException(
        string message,
        int errorCode,
        LockType lockType = LockType.Unknown,
        TimeSpan? timeout = null,
        string? sqlStatement = null)
        : base(message, errorCode, sqlStatement: sqlStatement)
    {
        LockType = lockType;
        Timeout = timeout;
        IsDatabaseLocked = errorCode == LibSqlErrorMessages.SQLITE_BUSY;
    }

    public LibSqlBusyException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Creates a busy exception with a timeout message.</summary>
    public static LibSqlBusyException CreateTimeoutException(TimeSpan timeout, string? sqlStatement = null)
    {
        var message = $"Database operation timed out after {timeout.TotalSeconds:F1} seconds waiting for lock to be released.";
        return new LibSqlBusyException(message, LibSqlErrorMessages.SQLITE_BUSY, LockType.Timeout, timeout, sqlStatement);
    }
}

/// <summary>Specifies the type of lock that caused a busy condition.</summary>
public enum LockType
{
    /// <summary>Unknown lock type.</summary>
    Unknown = 0,

    /// <summary>Database file is locked by another process.</summary>
    Database,

    /// <summary>A table is locked within the database.</summary>
    Table,

    /// <summary>Lock acquisition timed out.</summary>
    Timeout,

    /// <summary>Shared lock conflict.</summary>
    Shared,

    /// <summary>Reserved lock conflict.</summary>
    Reserved,

    /// <summary>Pending lock conflict.</summary>
    Pending,

    /// <summary>Exclusive lock conflict.</summary>
    Exclusive
}
