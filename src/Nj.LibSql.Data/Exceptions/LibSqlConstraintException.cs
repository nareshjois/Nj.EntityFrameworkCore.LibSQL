namespace Nj.LibSql.Data.Exceptions;

/// <summary>UNIQUE / FK / CHECK / NOT NULL constraint violations.</summary>
public class LibSqlConstraintException : LibSqlException
{
    /// <summary>Gets the type of constraint that was violated.</summary>
    public ConstraintType ConstraintType { get; }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConstraintException"/> class.</summary>
    public LibSqlConstraintException()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConstraintException"/> class.</summary>
    public LibSqlConstraintException(string message)
        : base(message)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConstraintException"/> class.</summary>
    public LibSqlConstraintException(string message, int errorCode)
        : base(message, errorCode)
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlConstraintException"/> class.</summary>
    public LibSqlConstraintException(string message, ConstraintType constraintType)
        : base(message)
        => ConstraintType = constraintType;

    /// <summary>Initializes a new instance of the <see cref="LibSqlConstraintException"/> class.</summary>
    public LibSqlConstraintException(string message, ConstraintType constraintType, string? sqlStatement = null)
        : base(message, LibSqlErrorMessages.SQLITE_CONSTRAINT, sqlStatement: sqlStatement)
        => ConstraintType = constraintType;

    /// <summary>Initializes a new instance of the <see cref="LibSqlConstraintException"/> class.</summary>
    public LibSqlConstraintException(string message, Exception innerException)
        : base(message, innerException)
    {
    }
}

/// <summary>Specifies the type of database constraint that was violated.</summary>
public enum ConstraintType
{
    /// <summary>Unknown constraint type.</summary>
    Unknown = 0,

    /// <summary>Primary key constraint violation.</summary>
    PrimaryKey,

    /// <summary>Unique constraint violation.</summary>
    Unique,

    /// <summary>Foreign key constraint violation.</summary>
    ForeignKey,

    /// <summary>Not null constraint violation.</summary>
    NotNull,

    /// <summary>Check constraint violation.</summary>
    Check,

    /// <summary>Row ID constraint violation.</summary>
    RowId
}
