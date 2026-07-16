namespace Nj.LibSql.Data.Exceptions;

/// <summary>Connection-time errors (open/connect failures, remote unreachable, auth, etc.).</summary>
public class LibSqlConnectionException : LibSqlException
{
    /// <summary>Connection string used when the error occurred, with secrets redacted.</summary>
    public string? ConnectionString { get; }

    public LibSqlConnectionException()
    {
    }

    public LibSqlConnectionException(string message)
        : base(message)
    {
    }

    public LibSqlConnectionException(string message, string? connectionString)
        : base(message)
        => ConnectionString = connectionString;

    public LibSqlConnectionException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LibSqlConnectionException(string message, string? connectionString, Exception innerException)
        : base(message, innerException)
        => ConnectionString = connectionString;

    public LibSqlConnectionException(
        string message,
        int errorCode,
        string? connectionString = null,
        Exception? innerException = null)
        : base(message, errorCode, innerException: innerException)
        => ConnectionString = connectionString;
}
