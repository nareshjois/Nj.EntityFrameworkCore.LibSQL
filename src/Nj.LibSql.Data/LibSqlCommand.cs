using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using Nj.LibSql.Bindings;
using Nj.LibSql.Data.Exceptions;
using Nj.LibSql.Data.Http;
using Nj.LibSql.Data.Internal;

namespace Nj.LibSql.Data;

/// <summary>Represents a SQL command to execute against a libSQL database.</summary>
public sealed class LibSqlCommand : DbCommand
{
    private LibSqlConnection? _connection;
    private string _commandText = string.Empty;
    private int _commandTimeout = 30;
    private LibSqlStatementHandle? _preparedStatement;
    private bool _isPrepared;
    private bool _enableStatementCaching = true;
    private LibSqlHttpCommand? _remoteCommand;

    /// <summary>Initializes a new instance of the <see cref="LibSqlCommand"/> class.</summary>
    public LibSqlCommand()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlCommand"/> class.</summary>
    public LibSqlCommand(string commandText)
        => CommandText = commandText;

    /// <summary>Initializes a new instance of the <see cref="LibSqlCommand"/> class.</summary>
    public LibSqlCommand(string commandText, LibSqlConnection connection)
        : this(commandText)
        => Connection = connection;

    /// <inheritdoc />
    [AllowNull]
    public override string CommandText
    {
        get => _commandText;
        set
        {
            var newValue = value ?? string.Empty;
            if (_commandText != newValue)
            {
                _commandText = newValue;
                ReleasePreparedStatement();
                if (_remoteCommand != null)
                {
                    _remoteCommand.CommandText = newValue;
                }
            }
        }
    }

    /// <inheritdoc />
    public override int CommandTimeout
    {
        get => _commandTimeout;
        set
        {
            _commandTimeout = value >= 0 ? value : throw new ArgumentOutOfRangeException(nameof(value));
            if (_remoteCommand != null)
            {
                _remoteCommand.CommandTimeout = _commandTimeout;
            }
        }
    }

    /// <inheritdoc />
    public override CommandType CommandType
    {
        get => CommandType.Text;
        set
        {
            if (value != CommandType.Text)
            {
                throw new NotSupportedException("libSQL only supports CommandType.Text.");
            }
        }
    }

    /// <inheritdoc />
    public new LibSqlConnection? Connection
    {
        get => _connection;
        set
        {
            if (!ReferenceEquals(_connection, value))
            {
                _connection?.RemoveCommand(this);
                _connection = value;
                _connection?.AddCommand(this);
                ReleasePreparedStatement();
                RecreateRemoteCommand();
            }
        }
    }

    /// <inheritdoc />
    protected override DbConnection? DbConnection
    {
        get => Connection;
        set => Connection = (LibSqlConnection?)value;
    }

    /// <inheritdoc />
    protected override DbParameterCollection DbParameterCollection => Parameters;

    /// <inheritdoc />
    public new LibSqlParameterCollection Parameters { get; } = new();

    /// <inheritdoc />
    protected override DbTransaction? DbTransaction { get; set; }

    /// <inheritdoc />
    public override bool DesignTimeVisible { get; set; }

    /// <summary>
    /// Gets or sets whether statement caching is enabled for this command. When true (default)
    /// and also enabled on the connection, prepared statements may be cached and reused.
    /// </summary>
    public bool EnableStatementCaching
    {
        get => _enableStatementCaching;
        set => _enableStatementCaching = value;
    }

    /// <inheritdoc />
    public override UpdateRowSource UpdatedRowSource { get; set; } = UpdateRowSource.None;

    /// <inheritdoc />
    public override void Cancel()
    {
        // Local native execute cannot abort mid-flight. Remote: best-effort via HTTP CTS.
        _remoteCommand?.Cancel();
    }

    /// <inheritdoc />
    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken)
    {
        EnsureConnectionOpen();
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        if (_remoteCommand != null)
        {
            SyncRemoteParameters();
            return await _remoteCommand.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
        }

        var result = ExecuteNonQuery();
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    /// <inheritdoc />
    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken)
    {
        EnsureConnectionOpen();
        cancellationToken.ThrowIfCancellationRequested();

        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        if (_remoteCommand != null)
        {
            SyncRemoteParameters();
            return await _remoteCommand.ExecuteScalarAsync(cancellationToken).ConfigureAwait(false);
        }

        var result = ExecuteScalar();
        cancellationToken.ThrowIfCancellationRequested();
        return result;
    }

    /// <inheritdoc />
    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        EnsureConnectionOpen();
        cancellationToken.ThrowIfCancellationRequested();

        if (_remoteCommand != null)
        {
            SyncRemoteParameters();
            var reader = await _remoteCommand
                .ExecuteReaderAsync(behavior, cancellationToken)
                .ConfigureAwait(false);
            return new LibSqlDataReader((LibSqlHttpDataReader)reader);
        }

        var local = ExecuteDbDataReader(behavior);
        cancellationToken.ThrowIfCancellationRequested();
        return local;
    }

    /// <inheritdoc />
    public override int ExecuteNonQuery()
    {
        EnsureConnectionOpen();

        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        if (_remoteCommand != null)
        {
            SyncRemoteParameters();
            return _remoteCommand.ExecuteNonQuery();
        }

        var connectionHandle = Connection!.ConnectionHandle!;
        IntPtr errorMsg;
        int result;

        if (_isPrepared && _preparedStatement != null)
        {
            var resetResult = LibSqlNative.libsql_reset_stmt(_preparedStatement, out var resetErrorMsg);
            if (resetResult != 0)
            {
                var resetError = LibSqlHelper.GetErrorMessage(resetErrorMsg);
                LibSqlNative.libsql_free_error_msg(resetErrorMsg);
                throw new InvalidOperationException($"Failed to reset prepared statement: {resetError}");
            }

            BindParameters(_preparedStatement);
            result = LibSqlNative.libsql_execute_stmt(_preparedStatement, out errorMsg);
        }
        else if (Parameters.Count > 0)
        {
            var statement = GetOrPrepareStatement(connectionHandle, out var usingCachedStatement);

            try
            {
                BindParameters(statement);
                result = LibSqlNative.libsql_execute_stmt(statement, out errorMsg);
            }
            finally
            {
                if (!usingCachedStatement)
                {
                    statement.Dispose();
                }
            }
        }
        else
        {
            // libsql_execute only runs the first statement and cannot run SELECTs.
            // EF Sqlite-style batches send multiple INSERTs / DDL in one CommandText.
            var statements = LibSqlStatementSplitter.Split(CommandText);
            if (statements.Count > 1)
            {
                var totalChanges = 0;
                foreach (var statement in statements)
                {
                    result = LibSqlNative.libsql_query(connectionHandle, statement, out var rowsHandle, out errorMsg);
                    if (result != 0)
                    {
                        var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
                        LibSqlNative.libsql_free_error_msg(errorMsg);
                        LibSqlErrorHandler.CheckResult(result, statement, errorMessage);
                    }

                    using (var rows = new LibSqlRowsHandle(rowsHandle))
                    {
                        while (true)
                        {
                            var nextResult = LibSqlNative.libsql_next_row(rows, out var rowPtr, out var nextErrorMsg);
                            if (nextResult != 0)
                            {
                                var errorMessage = LibSqlHelper.GetErrorMessage(nextErrorMsg);
                                if (nextErrorMsg != IntPtr.Zero)
                                {
                                    LibSqlNative.libsql_free_error_msg(nextErrorMsg);
                                }

                                LibSqlErrorHandler.CheckResult(nextResult, statement, errorMessage);
                            }

                            if (rowPtr == IntPtr.Zero)
                            {
                                break;
                            }

                            new LibSqlRowHandle(rowPtr).Dispose();
                        }
                    }

                    totalChanges += (int)LibSqlNative.libsql_changes(connectionHandle);
                }

                return totalChanges;
            }

            result = LibSqlNative.libsql_execute(connectionHandle, CommandText, out errorMsg);
        }

        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            LibSqlErrorHandler.CheckResult(result, CommandText, errorMessage);
        }

        return (int)LibSqlNative.libsql_changes(connectionHandle);
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
    {
        EnsureConnectionOpen();

        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        if (_remoteCommand != null)
        {
            SyncRemoteParameters();
            return _remoteCommand.ExecuteScalar();
        }

        var query = ExecuteNativeQuery(allowStatementCache: true);
        var closeBehavior = GetReaderCloseBehavior();
        using var reader = new LibSqlDataReader(query.Rows, CommandBehavior.SingleRow, query.OwnedStatement, closeBehavior);

        object? value = null;
        if (reader.Read() && reader.FieldCount > 0)
        {
            value = reader.GetValue(0);
            if (value == DBNull.Value)
            {
                value = null;
            }
        }

        // Always consume the rest of the result. Leaving an undrained COUNT(*)/PRAGMA
        // cursor across Close→Open leaves the database file locked (EF HasTables probe).
        while (reader.Read())
        {
        }

        return value;
    }

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
    {
        if (_remoteCommand != null)
        {
            EnsureConnectionOpen();
            SyncRemoteParameters();
            return new LibSqlDataReader((LibSqlHttpDataReader)_remoteCommand.ExecuteReader(behavior));
        }

        return ExecuteReader(behavior);
    }

    /// <inheritdoc />
    public new LibSqlDataReader ExecuteReader(CommandBehavior behavior = CommandBehavior.Default)
    {
        EnsureConnectionOpen();

        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        if (_remoteCommand != null)
        {
            SyncRemoteParameters();
            return new LibSqlDataReader((LibSqlHttpDataReader)_remoteCommand.ExecuteReader(behavior));
        }

        var query = ExecuteNativeQuery(allowStatementCache: false);
        var connectionHandle = Connection!.ConnectionHandle;
        LibSqlRowHandle? prefetchedRow = null;
        int recordsAffected;
        try
        {
            // Prefetch so UNIQUE/FK failures surface even when EF never calls Read()
            // (inserts without a result-set mapping).
            var nextResult = LibSqlNative.libsql_next_row(query.Rows, out var firstRowPtr, out var nextErrorMsg);
            if (nextResult != 0)
            {
                var errorMessage = LibSqlHelper.GetErrorMessage(nextErrorMsg);
                if (nextErrorMsg != IntPtr.Zero)
                {
                    LibSqlNative.libsql_free_error_msg(nextErrorMsg);
                }

                query.Rows.Dispose();
                query.OwnedStatement?.Dispose();
                LibSqlErrorHandler.CheckResult(nextResult, CommandText, errorMessage);
            }

            if (firstRowPtr != IntPtr.Zero)
            {
                prefetchedRow = new LibSqlRowHandle(firstRowPtr);
            }

            // Guard: under rare pool/close races the handle can be cleared before changes().
            recordsAffected = connectionHandle is { IsInvalid: false, IsClosed: false }
                ? (int)LibSqlNative.libsql_changes(connectionHandle)
                : 0;
            if (prefetchedRow is null && LibSqlNative.libsql_column_count(query.Rows) > 0)
            {
                recordsAffected = -1;
            }
            else if (prefetchedRow is null && recordsAffected == 0 && !IsDataManipulation(CommandText))
            {
                recordsAffected = -1;
            }
        }
        catch
        {
            prefetchedRow?.Dispose();
            query.Rows.Dispose();
            query.OwnedStatement?.Dispose();
            throw;
        }

        return new LibSqlDataReader(
            query.Rows,
            behavior,
            query.OwnedStatement,
            GetReaderCloseBehavior(),
            prefetchedRow,
            recordsAffected,
            rowWasPrefetched: true);
    }

    private static bool IsDataManipulation(string commandText)
    {
        var span = commandText.AsSpan().TrimStart();
        return span.StartsWith("INSERT", StringComparison.OrdinalIgnoreCase)
            || span.StartsWith("UPDATE", StringComparison.OrdinalIgnoreCase)
            || span.StartsWith("DELETE", StringComparison.OrdinalIgnoreCase)
            || span.StartsWith("REPLACE", StringComparison.OrdinalIgnoreCase);
    }

    /// <inheritdoc />
    public override void Prepare()
    {
        EnsureConnectionOpen();

        if (string.IsNullOrWhiteSpace(CommandText))
        {
            throw new InvalidOperationException("CommandText property has not been properly initialized.");
        }

        ReleasePreparedStatement();

        _preparedStatement = PrepareStatement(Connection!.ConnectionHandle!);
        _isPrepared = true;
    }

    /// <inheritdoc />
    protected override DbParameter CreateDbParameter()
        => new LibSqlParameter();

    /// <inheritdoc />
    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            _connection?.RemoveCommand(this);
            ReleasePreparedStatement();
            _remoteCommand?.Dispose();
            _remoteCommand = null;
        }

        base.Dispose(disposing);
    }

    private void RecreateRemoteCommand()
    {
        _remoteCommand?.Dispose();
        _remoteCommand = null;

        if (_connection is { IsRemoteConnection: true, HranaSession: { } session })
        {
            _remoteCommand = new LibSqlHttpCommand(session)
            {
                CommandText = _commandText,
                CommandTimeout = _commandTimeout,
            };
        }
    }

    private void SyncRemoteParameters()
    {
        if (_remoteCommand is null)
        {
            return;
        }

        _remoteCommand.Parameters.Clear();
        foreach (LibSqlParameter parameter in Parameters)
        {
            _remoteCommand.Parameters.Add(new LibSqlParameter(parameter.ParameterName, parameter.Value)
            {
                DbType = parameter.DbType,
                Size = parameter.Size,
                Direction = parameter.Direction,
                IsNullable = parameter.IsNullable,
                SourceColumn = parameter.SourceColumn,
            });
        }
    }

    private void EnsureConnectionOpen()
    {
        if (Connection is null)
        {
            throw new InvalidOperationException("Connection property has not been initialized.");
        }

        if (Connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection must be open to execute commands.");
        }

        if (Connection.IsRemoteConnection && _remoteCommand is null)
        {
            RecreateRemoteCommand();
        }
    }

    private void ReleasePreparedStatement()
    {
        if (_preparedStatement != null)
        {
            _preparedStatement.Dispose();
            _preparedStatement = null;
            _isPrepared = false;
        }
    }

    private NativeQuery ExecuteNativeQuery(bool allowStatementCache)
    {
        var connectionHandle = Connection!.ConnectionHandle!;
        LibSqlStatementHandle? ownedStatement = null;
        IntPtr rowsHandle;
        IntPtr errorMsg;
        int result;

        if (_isPrepared && _preparedStatement != null)
        {
            var resetResult = LibSqlNative.libsql_reset_stmt(_preparedStatement, out var resetErrorMsg);
            if (resetResult != 0)
            {
                var resetError = LibSqlHelper.GetErrorMessage(resetErrorMsg);
                LibSqlNative.libsql_free_error_msg(resetErrorMsg);
                throw new InvalidOperationException($"Failed to reset prepared statement: {resetError}");
            }

            BindParameters(_preparedStatement);
            result = LibSqlNative.libsql_query_stmt(_preparedStatement, out rowsHandle, out errorMsg);
        }
        else if (Parameters.Count > 0)
        {
            LibSqlStatementHandle statement;
            if (allowStatementCache)
            {
                statement = GetOrPrepareStatement(connectionHandle, out var usingCachedStatement);
                if (!usingCachedStatement)
                {
                    ownedStatement = statement;
                }
            }
            else
            {
                statement = PrepareStatement(connectionHandle);
                ownedStatement = statement;
            }

            try
            {
                BindParameters(statement);
                result = LibSqlNative.libsql_query_stmt(statement, out rowsHandle, out errorMsg);
            }
            catch
            {
                ownedStatement?.Dispose();
                throw;
            }
        }
        else
        {
            result = LibSqlNative.libsql_query(connectionHandle, CommandText, out rowsHandle, out errorMsg);
        }

        if (result != 0)
        {
            ownedStatement?.Dispose();
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            LibSqlErrorHandler.CheckResult(result, CommandText, errorMessage);
        }

        return new NativeQuery(new LibSqlRowsHandle(rowsHandle), ownedStatement);
    }

    private LibSqlStatementHandle PrepareStatement(LibSqlConnectionHandle connectionHandle)
    {
        var result = LibSqlNative.libsql_prepare(connectionHandle, CommandText, out var statementHandle, out var errorMsg);

        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw new InvalidOperationException($"Failed to prepare statement: {errorMessage}");
        }

        return new LibSqlStatementHandle(statementHandle);
    }

    private LibSqlDataReader.ReaderCloseBehavior GetReaderCloseBehavior()
        => SqlStatementClassifier.RequiresDrainOnReaderClose(CommandText)
            ? LibSqlDataReader.ReaderCloseBehavior.Drain
            : LibSqlDataReader.ReaderCloseBehavior.Release;

    private readonly record struct NativeQuery(LibSqlRowsHandle Rows, LibSqlStatementHandle? OwnedStatement);

    /// <summary>
    /// Binds parameters to the prepared statement. Named parameters (<c>@name</c>, <c>:name</c>,
    /// <c>$name</c>) are resolved by name via a positional map built from <see cref="CommandText"/>,
    /// matching SQLite's first-occurrence rule. When the SQL uses only anonymous <c>?</c> markers,
    /// parameters fall back to the order they were added to the collection.
    /// </summary>
    private void BindParameters(LibSqlStatementHandle statement)
    {
        Parameters.ValidateParameters();

        var parameterLayout = SqlParameterLayout.Parse(CommandText);

        foreach (var binding in parameterLayout.ResolveBindings(Parameters))
        {
            var parameter = binding.Parameter;
            var position = binding.Position;
            IntPtr errorMsg;
            int result;

            var libSqlValue = parameter.GetLibSqlValue();

            if (LibSqlTypeConverter.IsNull(libSqlValue))
            {
                result = LibSqlNative.libsql_bind_null(statement, position, out errorMsg);
            }
            else
            {
                switch (parameter.LibSqlType)
                {
                    case LibSqlDbType.Integer:
                        result = LibSqlNative.libsql_bind_int(statement, position, (long)libSqlValue, out errorMsg);
                        break;

                    case LibSqlDbType.Real:
                        result = LibSqlNative.libsql_bind_float(statement, position, (double)libSqlValue, out errorMsg);
                        break;

                    case LibSqlDbType.Text:
                        result = LibSqlNative.libsql_bind_string(statement, position, (string)libSqlValue, out errorMsg);
                        break;

                    case LibSqlDbType.Blob:
                        var blobValue = (byte[])libSqlValue;
                        var pinnedBlob = GCHandle.Alloc(blobValue, GCHandleType.Pinned);
                        try
                        {
                            result = LibSqlNative.libsql_bind_blob(
                                statement, position, pinnedBlob.AddrOfPinnedObject(), blobValue.Length, out errorMsg);
                        }
                        finally
                        {
                            pinnedBlob.Free();
                        }

                        break;

                    default:
                        throw new NotSupportedException($"Unsupported libSQL type: {parameter.LibSqlType}");
                }
            }

            if (result != 0)
            {
                var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
                LibSqlNative.libsql_free_error_msg(errorMsg);
                throw new InvalidOperationException(
                    $"Failed to bind parameter '{parameter.ParameterName}' (position {position}): {errorMessage}");
            }
        }
    }

    /// <summary>Gets or prepares a statement, using the cache if enabled.</summary>
    private LibSqlStatementHandle GetOrPrepareStatement(LibSqlConnectionHandle connectionHandle, out bool usingCachedStatement)
    {
        LibSqlStatementHandle? statement = null;
        usingCachedStatement = false;

        // Don't cache statements with positional parameters (?) as they're often used
        // for bulk operations where the same statement is executed many times.
        var hasPositionalParameters = CommandText.Contains('?') && !CommandText.Contains('@');

        if (_enableStatementCaching && Connection!.EnableStatementCaching && Connection.StatementCache != null && !hasPositionalParameters)
        {
            usingCachedStatement = Connection.StatementCache.TryGetStatement(CommandText, out statement);
        }

        if (!usingCachedStatement)
        {
            statement = PrepareStatement(connectionHandle);

            if (Connection!.EnableStatementCaching && Connection.StatementCache != null)
            {
                Connection.StatementCache.AddStatement(CommandText, statement);
                usingCachedStatement = true; // Don't dispose since it's now cached.
            }
        }
        else if (statement != null)
        {
            var resetResult = LibSqlNative.libsql_reset_stmt(statement, out var resetErrorMsg);
            if (resetResult != 0)
            {
                var resetError = LibSqlHelper.GetErrorMessage(resetErrorMsg);
                LibSqlNative.libsql_free_error_msg(resetErrorMsg);
                throw new InvalidOperationException($"Failed to reset cached statement: {resetError}");
            }
        }

        return statement!;
    }
}
