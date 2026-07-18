using System.Runtime.InteropServices;

namespace Nj.LibSql.Bindings;

/// <summary>
/// Native P/Invoke methods for the libsql client library.
/// </summary>
internal static partial class LibSqlNative
{
    private const string LibraryName = LibSqlNativeLibrary.LibraryName;

    /// <summary>
    /// Initializes the native library.
    /// </summary>
    /// <exception cref="InvalidOperationException">Thrown when the native library cannot be loaded.</exception>
    internal static void Initialize()
    {
        if (!LibSqlNativeLibrary.TryInitialize())
        {
            throw new InvalidOperationException(
                "Failed to load libsql native library. Please ensure the appropriate native library is available for your platform.");
        }
    }

    #region Database Management

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_file(string url, out IntPtr outDb, out IntPtr outErrMsg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_remote(string url, string authToken, out IntPtr outDb, out IntPtr outErrMsg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_remote_with_webpki(string url, string authToken, out IntPtr outDb, out IntPtr outErrMsg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_sync(
        string dbPath,
        string primaryUrl,
        string authToken,
        byte readYourWrites,
        string? encryptionKey,
        out IntPtr outDb,
        out IntPtr outErrMsg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_open_sync_with_webpki(
        string dbPath,
        string primaryUrl,
        string authToken,
        byte readYourWrites,
        string? encryptionKey,
        out IntPtr outDb,
        out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_open_sync_with_config(in LibSqlConfig config, out IntPtr outDb, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial void libsql_close(IntPtr db);

    #endregion

    #region Connection Management

    [LibraryImport(LibraryName)]
    internal static partial int libsql_connect(LibSqlDatabaseHandle db, out IntPtr outConn, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial void libsql_disconnect(IntPtr conn);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_reset(LibSqlConnectionHandle conn, out IntPtr outErrMsg);

    #endregion

    #region Statement Execution

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_prepare(LibSqlConnectionHandle conn, string sql, out IntPtr outStmt, out IntPtr outErrMsg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_execute(LibSqlConnectionHandle conn, string sql, out IntPtr outErrMsg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_query(LibSqlConnectionHandle conn, string sql, out IntPtr outRows, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_execute_stmt(LibSqlStatementHandle stmt, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_query_stmt(LibSqlStatementHandle stmt, out IntPtr outRows, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_reset_stmt(LibSqlStatementHandle stmt, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_stmt(IntPtr stmt);

    #endregion

    #region Parameter Binding

    [LibraryImport(LibraryName)]
    internal static partial int libsql_bind_int(LibSqlStatementHandle stmt, int idx, long value, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_bind_float(LibSqlStatementHandle stmt, int idx, double value, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_bind_null(LibSqlStatementHandle stmt, int idx, out IntPtr outErrMsg);

    [LibraryImport(LibraryName, StringMarshalling = StringMarshalling.Utf8)]
    internal static partial int libsql_bind_string(LibSqlStatementHandle stmt, int idx, string value, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_bind_blob(LibSqlStatementHandle stmt, int idx, IntPtr value, int valueLen, out IntPtr outErrMsg);

    #endregion

    #region Result Processing

    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_rows(IntPtr rows);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_column_count(LibSqlRowsHandle rows);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_column_name(LibSqlRowsHandle rows, int col, out IntPtr outName, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_column_type(LibSqlRowsHandle rows, LibSqlRowHandle row, int col, out int outType, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_next_row(LibSqlRowsHandle rows, out IntPtr outRow, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_row(IntPtr row);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_get_string(LibSqlRowHandle row, int col, out IntPtr outValue, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_string(IntPtr ptr);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_get_int(LibSqlRowHandle row, int col, out long outValue, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_get_float(LibSqlRowHandle row, int col, out double outValue, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial int libsql_get_blob(LibSqlRowHandle row, int col, out LibSqlBlob outBlob, out IntPtr outErrMsg);

    [LibraryImport(LibraryName)]
    internal static partial void libsql_free_blob(LibSqlBlob blob);

    #endregion

    #region Utility Functions

    [LibraryImport(LibraryName)]
    internal static partial ulong libsql_changes(LibSqlConnectionHandle conn);

    [LibraryImport(LibraryName)]
    internal static partial long libsql_last_insert_rowid(LibSqlConnectionHandle conn);

    #endregion

    #region Sync/Replication

    [LibraryImport(LibraryName)]
    internal static partial int libsql_sync2(LibSqlDatabaseHandle db, out LibSqlReplicated outReplicated, out IntPtr outErrMsg);

    #endregion

    #region Error Handling

    [LibraryImport(LibraryName, EntryPoint = "libsql_free_string")]
    internal static partial void libsql_free_error_msg(IntPtr errMsg);

    /// <summary>Returns the last error message for the database connection in English.</summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_errmsg")]
    internal static partial IntPtr sqlite3_errmsg(IntPtr db);

    /// <summary>Returns the extended error code for the most recent failed API call.</summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_extended_errcode")]
    internal static partial int sqlite3_extended_errcode(IntPtr db);

    /// <summary>Returns the error code for the most recent failed API call.</summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_errcode")]
    internal static partial int sqlite3_errcode(IntPtr db);

    #endregion

    #region Version Information

    /// <summary>Returns the libsql version string (e.g. "0.2.3").</summary>
    [LibraryImport(LibraryName)]
    internal static partial IntPtr libsql_libversion();

    /// <summary>Returns the SQLite version string (e.g. "3.45.1").</summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_libversion")]
    internal static partial IntPtr sqlite3_libversion();

    /// <summary>Returns the SQLite version number (e.g. 3045001).</summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_libversion_number")]
    internal static partial int sqlite3_libversion_number();

    /// <summary>Returns the SQLite source identifier string.</summary>
    [LibraryImport(LibraryName, EntryPoint = "sqlite3_sourceid")]
    internal static partial IntPtr sqlite3_sourceid();

    #endregion
}
