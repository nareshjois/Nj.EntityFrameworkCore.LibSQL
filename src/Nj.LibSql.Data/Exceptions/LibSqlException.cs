using System.Data.Common;

namespace Nj.LibSql.Data.Exceptions;

/// <summary>Errors from libSQL operations.</summary>
public class LibSqlException : DbException
{
    /// <summary>Gets the libSQL/SQLite error code.</summary>
    public int LibSqlErrorCode { get; }

    /// <summary>Gets the extended error code, if available.</summary>
    public int? ExtendedErrorCode { get; }

    /// <summary>Gets the SQL statement that caused the error, if available.</summary>
    public string? SqlStatement { get; }

    /// <summary>Gets additional context about the error.</summary>
    public string? ErrorContext { get; }

    public LibSqlException()
    {
    }

    public LibSqlException(string message)
        : base(message)
    {
    }

    public LibSqlException(string message, int errorCode)
        : base(message)
    {
        LibSqlErrorCode = errorCode;
        HResult = errorCode;
    }

    public LibSqlException(string message, Exception innerException)
        : base(message, innerException)
    {
    }

    public LibSqlException(
        string message,
        int errorCode,
        int? extendedErrorCode = null,
        string? sqlStatement = null,
        string? errorContext = null,
        Exception? innerException = null)
        : base(message, innerException)
    {
        LibSqlErrorCode = errorCode;
        HResult = errorCode;
        ExtendedErrorCode = extendedErrorCode;
        SqlStatement = sqlStatement;
        ErrorContext = errorContext;
    }

    /// <summary>Creates a <see cref="LibSqlException"/> from a native error code.</summary>
    public static LibSqlException FromErrorCode(
        int errorCode,
        string? customMessage = null,
        string? sqlStatement = null,
        string? errorContext = null)
    {
        var message = customMessage ?? LibSqlErrorMessages.GetErrorMessage(errorCode);
        return new LibSqlException(message, errorCode, sqlStatement: sqlStatement, errorContext: errorContext);
    }
}
