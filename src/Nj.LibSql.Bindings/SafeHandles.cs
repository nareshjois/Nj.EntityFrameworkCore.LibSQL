using Microsoft.Win32.SafeHandles;

namespace Nj.LibSql.Bindings;

/// <summary>
/// Base class for all libsql safe handles.
/// </summary>
internal abstract class LibSqlSafeHandle : SafeHandleZeroOrMinusOneIsInvalid
{
    protected LibSqlSafeHandle()
        : base(true)
    {
    }

    protected LibSqlSafeHandle(IntPtr handle)
        : base(true)
        => SetHandle(handle);
}

/// <summary>
/// Safe handle for <c>libsql_database_t</c>.
/// </summary>
internal sealed class LibSqlDatabaseHandle : LibSqlSafeHandle
{
    public LibSqlDatabaseHandle()
    {
    }

    public LibSqlDatabaseHandle(IntPtr handle)
        : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSqlNative.libsql_close(handle);
        }

        return true;
    }
}

/// <summary>
/// Safe handle for <c>libsql_connection_t</c>.
/// </summary>
internal sealed class LibSqlConnectionHandle : LibSqlSafeHandle
{
    public LibSqlConnectionHandle()
    {
    }

    public LibSqlConnectionHandle(IntPtr handle)
        : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSqlNative.libsql_disconnect(handle);
        }

        return true;
    }
}

/// <summary>
/// Safe handle for <c>libsql_stmt_t</c>.
/// </summary>
internal sealed class LibSqlStatementHandle : LibSqlSafeHandle
{
    public LibSqlStatementHandle()
    {
    }

    public LibSqlStatementHandle(IntPtr handle)
        : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSqlNative.libsql_free_stmt(handle);
        }

        return true;
    }
}

/// <summary>
/// Safe handle for <c>libsql_rows_t</c>.
/// </summary>
internal sealed class LibSqlRowsHandle : LibSqlSafeHandle
{
    public LibSqlRowsHandle()
    {
    }

    public LibSqlRowsHandle(IntPtr handle)
        : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSqlNative.libsql_free_rows(handle);
        }

        return true;
    }
}

/// <summary>
/// Safe handle for <c>libsql_row_t</c>.
/// </summary>
internal sealed class LibSqlRowHandle : LibSqlSafeHandle
{
    public LibSqlRowHandle()
    {
    }

    public LibSqlRowHandle(IntPtr handle)
        : base(handle)
    {
    }

    protected override bool ReleaseHandle()
    {
        if (!IsInvalid && !IsClosed)
        {
            LibSqlNative.libsql_free_row(handle);
        }

        return true;
    }
}
