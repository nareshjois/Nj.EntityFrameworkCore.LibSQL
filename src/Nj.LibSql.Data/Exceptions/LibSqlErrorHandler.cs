namespace Nj.LibSql.Data.Exceptions;

/// <summary>
/// Provides centralized error handling and exception mapping for libSQL operations.
/// </summary>
internal static class LibSqlErrorHandler
{
    /// <summary>
    /// Checks the result code and throws an appropriate exception if it indicates an error.
    /// </summary>
    public static void CheckResult(int result, string? sqlStatement = null, string? errorContext = null)
    {
        if (result is LibSqlErrorMessages.SQLITE_OK or LibSqlErrorMessages.SQLITE_ROW or LibSqlErrorMessages.SQLITE_DONE)
        {
            return;
        }

        throw CreateException(result, sqlStatement, errorContext);
    }

    /// <summary>Creates an appropriate exception based on the error code.</summary>
    public static LibSqlException CreateException(int errorCode, string? sqlStatement = null, string? errorContext = null)
    {
        var message = !string.IsNullOrWhiteSpace(errorContext)
            ? errorContext!
            : LibSqlErrorMessages.GetErrorMessage(errorCode);

        var baseErrorCode = errorCode & 0xFF;

        // libSQL may surface constraint failures as SQLITE_ERROR / SQLITE_INTERNAL with a
        // message like "UNIQUE constraint failed: t.u" instead of SQLITE_CONSTRAINT.
        var messageConstraintType = TryParseConstraintTypeFromMessage(message);
        if (messageConstraintType is { } parsedConstraint
            && baseErrorCode is not LibSqlErrorMessages.SQLITE_BUSY
                and not LibSqlErrorMessages.SQLITE_LOCKED)
        {
            return new LibSqlConstraintException(message, parsedConstraint, sqlStatement);
        }

        switch (baseErrorCode)
        {
            case LibSqlErrorMessages.SQLITE_BUSY:
            case LibSqlErrorMessages.SQLITE_LOCKED:
                var lockType = baseErrorCode == LibSqlErrorMessages.SQLITE_BUSY ? LockType.Database : LockType.Table;
                return new LibSqlBusyException(message, errorCode, lockType, sqlStatement: sqlStatement);

            case LibSqlErrorMessages.SQLITE_CONSTRAINT:
                return new LibSqlConstraintException(message, ConstraintType.Unknown, sqlStatement);

            case LibSqlErrorMessages.SQLITE_CANTOPEN:
            case LibSqlErrorMessages.SQLITE_NOTADB:
            case LibSqlErrorMessages.SQLITE_AUTH:
            case LibSqlErrorMessages.SQLITE_PERM:
                return new LibSqlConnectionException(message, errorCode);

            case LibSqlErrorMessages.SQLITE_CORRUPT:
            case LibSqlErrorMessages.SQLITE_FORMAT:
                return new LibSqlException($"Database corruption detected: {message}", errorCode, errorCode, sqlStatement, errorContext);

            default:
                return new LibSqlException(message, errorCode, errorCode, sqlStatement, errorContext);
        }
    }

    /// <summary>
    /// Infers a constraint type from SQLite/libSQL error message text when the result code
    /// is not <c>SQLITE_CONSTRAINT</c>.
    /// </summary>
    private static ConstraintType? TryParseConstraintTypeFromMessage(string message)
    {
        if (message.Contains("UNIQUE constraint failed", StringComparison.OrdinalIgnoreCase)
            || message.Contains("PRIMARY KEY must be unique", StringComparison.OrdinalIgnoreCase))
        {
            return message.Contains("PRIMARY KEY", StringComparison.OrdinalIgnoreCase)
                ? ConstraintType.PrimaryKey
                : ConstraintType.Unique;
        }

        if (message.Contains("FOREIGN KEY constraint failed", StringComparison.OrdinalIgnoreCase))
        {
            return ConstraintType.ForeignKey;
        }

        if (message.Contains("NOT NULL constraint failed", StringComparison.OrdinalIgnoreCase))
        {
            return ConstraintType.NotNull;
        }

        if (message.Contains("CHECK constraint failed", StringComparison.OrdinalIgnoreCase))
        {
            return ConstraintType.Check;
        }

        return null;
    }
}
