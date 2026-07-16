namespace Nj.LibSql.Data;

/// <summary>
/// Represents the libSQL data types that correspond to SQLite storage classes.
/// </summary>
public enum LibSqlDbType
{
    /// <summary>Integer type (64-bit signed integer).</summary>
    Integer,

    /// <summary>Real/floating point type (64-bit IEEE floating point number).</summary>
    Real,

    /// <summary>Text type (UTF-8 encoded string).</summary>
    Text,

    /// <summary>BLOB type (binary large object / byte array).</summary>
    Blob,

    /// <summary>NULL type.</summary>
    Null
}
