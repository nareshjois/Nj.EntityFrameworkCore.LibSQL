namespace Nj.LibSql.Data.Exceptions;

/// <summary>Connection-time errors (open/connect failures, remote unreachable, auth, etc.).</summary>
public class LibSqlConnectionException : LibSqlException
{
    /// <summary>Connection string used when the error occurred, with secrets redacted.</summary>
    public string? ConnectionString { get; }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConnectionException"/> class.</summary>
    public LibSqlConnectionException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConnectionException"/> class.</summary>
    public LibSqlConnectionException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConnectionException"/> class.</summary>
    public LibSqlConnectionException(string message, string? connectionString)
        : base(message)
        => ConnectionString = connectionString;

    /// <summary>Initializes a new instance of the <see cref="LibSqlConnectionException"/> class.</summary>
    public LibSqlConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConnectionException"/> class.</summary>
    public LibSqlConnectionException(string message, string? connectionString, Exception innerException)
        : base(message, innerException)
        => ConnectionString = connectionString;

    /// <summary>Initializes a new instance of the <see cref="LibSqlConnectionException"/> class.</summary>
    public LibSqlConnectionException(
        string message,
        int errorCode,
        string? connectionString = null,
        Exception? innerException = null)
        : base(message, errorCode, innerException: innerException)
        => ConnectionString = connectionString;
}
