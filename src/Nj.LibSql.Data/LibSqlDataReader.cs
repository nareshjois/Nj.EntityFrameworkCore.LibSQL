using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Nj.LibSql.Bindings;
using Nj.LibSql.Data.Exceptions;
using Nj.LibSql.Data.Http;
using Nj.LibSql.Data.Internal;

namespace Nj.LibSql.Data;

/// <summary>Provides a way of reading a forward-only stream of rows from a libSQL database.</summary>
public sealed class LibSqlDataReader : DbDataReader
{
    internal enum ReaderCloseBehavior
    {
        Release,
        Drain,
    }

    private enum LibSqlColumnType
    {
        Integer = 1,
        Real = 2,
        Text = 3,
        Blob = 4,
        Null = 5,
    }

    private readonly LibSqlRowsHandle? _rowsHandle;
    private readonly LibSqlHttpDataReader? _httpDataReader;
    private readonly LibSqlStatementHandle? _ownedStatement;
    private bool _isHttpReader;
    private LibSqlRowHandle? _currentRow;
    private LibSqlRowHandle? _prefetchedRow;
    private bool _hasPrefetchedRow;
    private bool _atEnd;
    private readonly int _recordsAffected;
    private bool _disposed;
    private bool _closed;
    private int _fieldCount = -1;
    private string[]? _columnNames;
    private bool _hasInitializedMetadata;

    /// <summary>Initializes a closed reader, for testing purposes.</summary>
    public LibSqlDataReader()
    {
        _closed = true;
        _recordsAffected = -1;
    }

    /// <param name="rowsHandle">The handle to the libSQL rows result set.</param>
    /// <param name="behavior">Unused placeholder for future <see cref="CommandBehavior"/> support.</param>
    /// <param name="ownedStatement">The prepared statement whose ownership transfers to the reader.</param>
    /// <param name="closeBehavior">Whether closing releases the result immediately or drains it first.</param>
    /// <param name="prefetchedRow">
    /// Optional first row already stepped from the result set. Used so DML constraint failures
    /// surface during <c>ExecuteReader</c> even when the caller never calls <see cref="Read"/>.
    /// </param>
    /// <param name="recordsAffected">
    /// Rows changed by the statement (from <c>libsql_changes</c>), or -1 when unknown/SELECT.
    /// </param>
    /// <param name="rowWasPrefetched">
    /// When true, <paramref name="prefetchedRow"/> reflects an already-executed <c>libsql_next_row</c>
    /// (null means end of rows / completed DML).
    /// </param>
    internal LibSqlDataReader(
        LibSqlRowsHandle rowsHandle,
        CommandBehavior behavior = CommandBehavior.Default,
        LibSqlStatementHandle? ownedStatement = null,
        ReaderCloseBehavior closeBehavior = ReaderCloseBehavior.Release,
        LibSqlRowHandle? prefetchedRow = null,
        int recordsAffected = -1,
        bool rowWasPrefetched = false)
    {
        _rowsHandle = rowsHandle ?? throw new ArgumentNullException(nameof(rowsHandle));
        _ownedStatement = ownedStatement;
        _closed = false;
        _isHttpReader = false;
        _prefetchedRow = prefetchedRow;
        _hasPrefetchedRow = prefetchedRow is not null;
        _atEnd = rowWasPrefetched && prefetchedRow is null;
        _recordsAffected = recordsAffected;
        _ = behavior;
        _ = closeBehavior;
    }

    internal LibSqlDataReader(LibSqlHttpDataReader httpDataReader, CommandBehavior behavior = CommandBehavior.Default)
    {
        _httpDataReader = httpDataReader ?? throw new ArgumentNullException(nameof(httpDataReader));
        _closed = false;
        _isHttpReader = true;
        _recordsAffected = -1;
        _ = behavior;
    }

    public override int Depth => 0;

    public override int FieldCount
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isHttpReader && _httpDataReader != null)
            {
                return _httpDataReader.FieldCount;
            }

            if (_closed || _rowsHandle == null)
            {
                return 0;
            }

            EnsureMetadataInitialized();
            return _fieldCount;
        }
    }

    public override bool HasRows
    {
        get
        {
            ObjectDisposedException.ThrowIf(_disposed, this);

            if (_isHttpReader && _httpDataReader != null)
            {
                return _httpDataReader.HasRows;
            }

            if (_closed || _rowsHandle == null)
            {
                return false;
            }

            return _hasPrefetchedRow || _currentRow != null;
        }
    }

    public override bool IsClosed => _closed || _disposed;

    public override int RecordsAffected
        => _isHttpReader && _httpDataReader != null
            ? _httpDataReader.RecordsAffected
            : _recordsAffected;

    public override object this[int ordinal] => GetValue(ordinal);

    public override object this[string name] => GetValue(GetOrdinal(name));

    public override void Close()
    {
        if (_closed)
        {
            return;
        }

        if (_isHttpReader && _httpDataReader != null)
        {
            _closed = true;
            _httpDataReader.Close();
            return;
        }

        try
        {
            // Always step to EOF before free_rows. Leaving an undrained cursor (e.g. COUNT(*),
            // typeof(max(...)) inference, or SingleRow) can keep the table/file locked across
            // subsequent DDL on the same connection.
            if (!_disposed && _rowsHandle != null && !_rowsHandle.IsInvalid && !_atEnd)
            {
                while (Read())
                {
                }
            }
        }
        finally
        {
            _closed = true;

            _currentRow?.Dispose();
            _currentRow = null;

            _prefetchedRow?.Dispose();
            _prefetchedRow = null;
            _hasPrefetchedRow = false;

            _rowsHandle?.Dispose();

            // Parameterized queries transfer their statement to the reader.
            _ownedStatement?.Dispose();
        }
    }

    public override bool GetBoolean(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to boolean.");
        }

        return (bool)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(bool));
    }

    public override byte GetByte(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to byte.");
        }

        return (byte)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(byte));
    }

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closed || _rowsHandle == null || _currentRow == null)
        {
            throw new InvalidOperationException("No current row available. Call Read() first.");
        }

        ValidateOrdinal(ordinal);

        var data = GetBlobBytes(ordinal);

        if (buffer == null)
        {
            return data.Length;
        }

        var actualLength = Math.Min(length, data.Length - dataOffset);
        if (actualLength <= 0)
        {
            return 0;
        }

        Array.Copy(data, dataOffset, buffer, bufferOffset, actualLength);
        return actualLength;
    }

    public override char GetChar(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to char.");
        }

        return (char)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(char));
    }

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            return 0;
        }

        var str = (string)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(string));

        if (buffer == null)
        {
            return str.Length;
        }

        var charsToRead = Math.Min(str.Length - dataOffset, length);
        charsToRead = Math.Min(charsToRead, buffer.Length - bufferOffset);

        if (charsToRead > 0)
        {
            str.CopyTo((int)dataOffset, buffer, bufferOffset, (int)charsToRead);
        }

        return charsToRead;
    }

    public override string GetDataTypeName(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.GetDataTypeName(ordinal);
        }

        if (_closed || _rowsHandle == null)
        {
            throw new InvalidOperationException("Reader is closed.");
        }

        ValidateOrdinal(ordinal);

        if (_currentRow != null)
        {
            var result = LibSqlNative.libsql_column_type(_rowsHandle, _currentRow, ordinal, out var columnType, out var errorMsg);
            if (result == 0)
            {
                return columnType switch
                {
                    (int)LibSqlColumnType.Integer => "INTEGER",
                    (int)LibSqlColumnType.Real => "REAL",
                    (int)LibSqlColumnType.Text => "TEXT",
                    (int)LibSqlColumnType.Blob => "BLOB",
                    (int)LibSqlColumnType.Null => "NULL",
                    _ => "UNKNOWN"
                };
            }

            LibSqlNative.libsql_free_error_msg(errorMsg);
        }

        return "UNKNOWN";
    }

    public override DateTime GetDateTime(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to DateTime.");
        }

        return (DateTime)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(DateTime));
    }

    public override decimal GetDecimal(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to decimal.");
        }

        return (decimal)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(decimal));
    }

    public override double GetDouble(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.GetDouble(ordinal);
        }

        if (_closed || _rowsHandle == null || _currentRow == null)
        {
            throw new InvalidOperationException("No current row available. Call Read() first.");
        }

        ValidateOrdinal(ordinal);

        var result = LibSqlNative.libsql_get_float(_currentRow, ordinal, out var value, out var errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get double value: {errorMessage}");
        }

        return value;
    }

    public override IEnumerator GetEnumerator()
        => new DbEnumerator(this);

    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.GetFieldType(ordinal);
        }

        if (_closed || _rowsHandle == null)
        {
            throw new InvalidOperationException("Reader is closed.");
        }

        ValidateOrdinal(ordinal);

        if (_currentRow != null)
        {
            var result = LibSqlNative.libsql_column_type(_rowsHandle, _currentRow, ordinal, out var columnType, out var errorMsg);
            if (result == 0)
            {
                return columnType switch
                {
                    (int)LibSqlColumnType.Integer => typeof(long),
                    (int)LibSqlColumnType.Real => typeof(double),
                    (int)LibSqlColumnType.Text => typeof(string),
                    (int)LibSqlColumnType.Blob => typeof(byte[]),
                    (int)LibSqlColumnType.Null => typeof(object),
                    _ => typeof(object)
                };
            }

            LibSqlNative.libsql_free_error_msg(errorMsg);
        }

        return typeof(object);
    }

    public override float GetFloat(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to float.");
        }

        return (float)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(float));
    }

    public override Guid GetGuid(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to Guid.");
        }

        return (Guid)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(Guid));
    }

    public override short GetInt16(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to short.");
        }

        return (short)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(short));
    }

    public override int GetInt32(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
        {
            throw new InvalidCastException("Cannot convert NULL to int.");
        }

        return (int)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(int));
    }

    public override long GetInt64(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.GetInt64(ordinal);
        }

        if (_closed || _rowsHandle == null || _currentRow == null)
        {
            throw new InvalidOperationException("No current row available. Call Read() first.");
        }

        ValidateOrdinal(ordinal);

        var result = LibSqlNative.libsql_get_int(_currentRow, ordinal, out var value, out var errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get integer value: {errorMessage}");
        }

        return value;
    }

    public override string GetName(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.GetName(ordinal);
        }

        if (_closed || _rowsHandle == null)
        {
            throw new InvalidOperationException("Reader is closed.");
        }

        EnsureMetadataInitialized();
        ValidateOrdinal(ordinal);

        return _columnNames![ordinal];
    }

    public override int GetOrdinal(string name)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.GetOrdinal(name);
        }

        if (_closed || _rowsHandle == null)
        {
            throw new InvalidOperationException("Reader is closed.");
        }

        if (string.IsNullOrEmpty(name))
        {
            throw new ArgumentException("Column name cannot be null or empty.", nameof(name));
        }

        EnsureMetadataInitialized();

        for (var i = 0; i < _columnNames!.Length; i++)
        {
            if (string.Equals(_columnNames[i], name, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        throw new IndexOutOfRangeException($"Column '{name}' not found.");
    }

    public override string GetString(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.GetString(ordinal);
        }

        if (_closed || _rowsHandle == null || _currentRow == null)
        {
            throw new InvalidOperationException("No current row available. Call Read() first.");
        }

        ValidateOrdinal(ordinal);

        var result = LibSqlNative.libsql_get_string(_currentRow, ordinal, out var strPtr, out var errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get string value: {errorMessage}");
        }

        try
        {
            return Marshal.PtrToStringUTF8(strPtr) ?? string.Empty;
        }
        finally
        {
            if (strPtr != IntPtr.Zero)
            {
                LibSqlNative.libsql_free_string(strPtr);
            }
        }
    }

    public override object GetValue(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.GetValue(ordinal);
        }

        if (_closed || _rowsHandle == null || _currentRow == null)
        {
            throw new InvalidOperationException("No current row available. Call Read() first.");
        }

        ValidateOrdinal(ordinal);

        var result = LibSqlNative.libsql_column_type(_rowsHandle, _currentRow, ordinal, out var columnType, out var errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get column type: {errorMessage}");
        }

        return columnType switch
        {
            (int)LibSqlColumnType.Integer => GetInt64(ordinal),
            (int)LibSqlColumnType.Real => GetDouble(ordinal),
            (int)LibSqlColumnType.Text => GetString(ordinal),
            (int)LibSqlColumnType.Blob => GetBlobBytes(ordinal),
            (int)LibSqlColumnType.Null => DBNull.Value,
            _ => throw new NotSupportedException($"Unknown column type: {columnType}")
        };
    }

    public override int GetValues(object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var count = Math.Min(values.Length, FieldCount);
        for (var i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }

        return count;
    }

    public override bool IsDBNull(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.IsDBNull(ordinal);
        }

        if (_closed || _rowsHandle == null || _currentRow == null)
        {
            throw new InvalidOperationException("No current row available. Call Read() first.");
        }

        ValidateOrdinal(ordinal);

        var result = LibSqlNative.libsql_column_type(_rowsHandle, _currentRow, ordinal, out var columnType, out var errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get column type: {errorMessage}");
        }

        return columnType == (int)LibSqlColumnType.Null;
    }

    public override bool NextResult()
        // libSQL does not support multiple result sets from a single query execution.
        => false;

    [UnconditionalSuppressMessage(
        "ReflectionAnalysis",
        "IL2111",
        Justification = "ADO.NET schema tables require a System.Type-valued DataType column; values are fixed BCL type literals.")]
    public override DataTable? GetSchemaTable()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closed || _rowsHandle == null)
        {
            return null;
        }

        EnsureMetadataInitialized();

        var schemaTable = new DataTable("SchemaTable");

        schemaTable.Columns.Add("ColumnName", typeof(string));
        schemaTable.Columns.Add("ColumnOrdinal", typeof(int));
        schemaTable.Columns.Add("ColumnSize", typeof(int));
        schemaTable.Columns.Add("NumericPrecision", typeof(short));
        schemaTable.Columns.Add("NumericScale", typeof(short));
        schemaTable.Columns.Add("DataType", typeof(Type));
        schemaTable.Columns.Add("ProviderType", typeof(string));
        schemaTable.Columns.Add("IsLong", typeof(bool));
        schemaTable.Columns.Add("AllowDBNull", typeof(bool));
        schemaTable.Columns.Add("IsReadOnly", typeof(bool));
        schemaTable.Columns.Add("IsRowVersion", typeof(bool));
        schemaTable.Columns.Add("IsUnique", typeof(bool));
        schemaTable.Columns.Add("IsKey", typeof(bool));
        schemaTable.Columns.Add("IsAutoIncrement", typeof(bool));
        schemaTable.Columns.Add("BaseSchemaName", typeof(string));
        schemaTable.Columns.Add("BaseCatalogName", typeof(string));
        schemaTable.Columns.Add("BaseTableName", typeof(string));
        schemaTable.Columns.Add("BaseColumnName", typeof(string));

        for (var i = 0; i < _fieldCount; i++)
        {
            var row = schemaTable.NewRow();

            row["ColumnName"] = _columnNames![i];
            row["ColumnOrdinal"] = i;
            row["ColumnSize"] = -1;
            row["NumericPrecision"] = DBNull.Value;
            row["NumericScale"] = DBNull.Value;

            var dataType = typeof(object);
            var providerType = "UNKNOWN";

            if (_currentRow != null)
            {
                var result = LibSqlNative.libsql_column_type(_rowsHandle, _currentRow, i, out var columnType, out var errorMsg);
                if (result == 0)
                {
                    (dataType, providerType) = columnType switch
                    {
                        (int)LibSqlColumnType.Integer => (typeof(long), "INTEGER"),
                        (int)LibSqlColumnType.Real => (typeof(double), "REAL"),
                        (int)LibSqlColumnType.Text => (typeof(string), "TEXT"),
                        (int)LibSqlColumnType.Blob => (typeof(byte[]), "BLOB"),
                        (int)LibSqlColumnType.Null => (typeof(object), "NULL"),
                        _ => (typeof(object), "UNKNOWN")
                    };
                }
                else
                {
                    LibSqlNative.libsql_free_error_msg(errorMsg);
                }
            }

            row["DataType"] = dataType;
            row["ProviderType"] = providerType;
            row["IsLong"] = providerType == "BLOB";
            row["AllowDBNull"] = true;
            row["IsReadOnly"] = true;
            row["IsRowVersion"] = false;
            row["IsUnique"] = false;
            row["IsKey"] = false;
            row["IsAutoIncrement"] = false;
            row["BaseSchemaName"] = DBNull.Value;
            row["BaseCatalogName"] = DBNull.Value;
            row["BaseTableName"] = DBNull.Value;
            row["BaseColumnName"] = _columnNames![i];

            schemaTable.Rows.Add(row);
        }

        return schemaTable;
    }

    public override T GetFieldValue<T>(int ordinal)
    {
        var value = GetValue(ordinal);

        if (value == DBNull.Value)
        {
            if (typeof(T).IsValueType && Nullable.GetUnderlyingType(typeof(T)) == null)
            {
                throw new InvalidCastException(
                    $"Column contains null value and cannot be converted to non-nullable type {typeof(T)}.");
            }

            return default!;
        }

        try
        {
            return (T)LibSqlTypeConverter.ConvertFromLibSql(value, typeof(T));
        }
        catch (Exception ex)
        {
            throw new InvalidCastException($"Cannot convert value of type {value.GetType()} to {typeof(T)}.", ex);
        }
    }

    public override bool Read()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_isHttpReader && _httpDataReader != null)
        {
            return _httpDataReader.Read();
        }

        if (_closed || _rowsHandle == null)
        {
            return false;
        }

        _currentRow?.Dispose();
        _currentRow = null;

        // Serve the row prefetched during ExecuteReader (surfaces DML errors before Read).
        if (_hasPrefetchedRow)
        {
            _hasPrefetchedRow = false;
            _currentRow = _prefetchedRow;
            _prefetchedRow = null;
            if (_currentRow == null)
            {
                _atEnd = true;
                return false;
            }

            return true;
        }

        if (_atEnd)
        {
            return false;
        }

        // End of rows: result == 0 && rowPtr == Zero.
        // Constraint / other failures (e.g. UNIQUE on INSERT…RETURNING): result != 0 with errMsg set.
        // Do not treat result != 0 as EOF — that silently drops constraint violations.
        var result = LibSqlNative.libsql_next_row(_rowsHandle, out var rowPtr, out var errorMsg);

        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            if (errorMsg != IntPtr.Zero)
            {
                LibSqlNative.libsql_free_error_msg(errorMsg);
            }

            LibSqlErrorHandler.CheckResult(result, errorContext: errorMessage);
            return false;
        }

        if (rowPtr == IntPtr.Zero)
        {
            _atEnd = true;
            return false;
        }

        _currentRow = new LibSqlRowHandle(rowPtr);
        return true;
    }

    private void EnsureMetadataInitialized()
    {
        if (_hasInitializedMetadata || _rowsHandle == null)
        {
            return;
        }

        _fieldCount = LibSqlNative.libsql_column_count(_rowsHandle);
        _columnNames = new string[_fieldCount];

        for (var i = 0; i < _fieldCount; i++)
        {
            var result = LibSqlNative.libsql_column_name(_rowsHandle, i, out var namePtr, out var errorMsg);
            if (result != 0)
            {
                var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
                LibSqlNative.libsql_free_error_msg(errorMsg);
                throw new InvalidOperationException($"Failed to get column name: {errorMessage}");
            }

            try
            {
                _columnNames[i] = Marshal.PtrToStringUTF8(namePtr) ?? $"Column{i}";
            }
            finally
            {
                if (namePtr != IntPtr.Zero)
                {
                    LibSqlNative.libsql_free_string(namePtr);
                }
            }
        }

        _hasInitializedMetadata = true;
    }

    private void ValidateOrdinal(int ordinal)
    {
        if (ordinal < 0 || ordinal >= FieldCount)
        {
            throw new IndexOutOfRangeException($"Column ordinal {ordinal} is out of range. Valid range is 0 to {FieldCount - 1}.");
        }
    }

    private byte[] GetBlobBytes(int ordinal)
    {
        ObjectDisposedException.ThrowIf(_disposed, this);
        if (_closed || _rowsHandle == null || _currentRow == null)
        {
            throw new InvalidOperationException("No current row available. Call Read() first.");
        }

        ValidateOrdinal(ordinal);

        var result = LibSqlNative.libsql_get_blob(_currentRow, ordinal, out var blob, out var errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to get blob value: {errorMessage}");
        }

        try
        {
            // Empty BLOBs are distinct from NULL (handled via LibSqlColumnType.Null).
            // ADO.NET / EF Core expect Array.Empty<byte>() for zero-length blobs, not null.
            if (blob.Len == 0 || blob.Ptr == IntPtr.Zero)
            {
                return [];
            }

            var data = new byte[blob.Len];
            Marshal.Copy(blob.Ptr, data, 0, blob.Len);
            return data;
        }
        finally
        {
            LibSqlNative.libsql_free_blob(blob);
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!_disposed)
        {
            if (disposing)
            {
                Close();
                if (_isHttpReader && _httpDataReader != null)
                {
                    _httpDataReader.Dispose();
                }
            }

            _disposed = true;
        }

        base.Dispose(disposing);
    }
}
