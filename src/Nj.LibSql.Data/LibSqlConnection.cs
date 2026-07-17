using System.Collections.Concurrent;
using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Threading;
using Nj.LibSql.Bindings;
using Nj.LibSql.Data.Exceptions;
using Nj.LibSql.Data.Http;
using Nj.LibSql.Data.Internal;

namespace Nj.LibSql.Data;

/// <summary>
/// Represents a connection to a libSQL database (local file / <c>:memory:</c>,
/// remote Hrana HTTP/WSS, or embedded replica).
/// </summary>
public sealed class LibSqlConnection : DbConnection
{
    private static readonly ConcurrentDictionary<LibSqlConnection, byte> OpenConnections = new();

    private readonly object _lockObject = new();
    private readonly List<WeakReference<LibSqlCommand>> _commands = [];
    private LibSqlDatabaseHandle? _databaseHandle;
    private LibSqlConnectionHandle? _connectionHandle;
    private ILibSqlHranaSession? _hranaSession;
    private ConnectionState _connectionState = ConnectionState.Closed;
    private string _connectionString = string.Empty;
    private LibSqlConnectionStringBuilder? _connectionStringBuilder;
    private LibSqlStatementCache? _statementCache;
    private bool _enableStatementCaching;
    private bool _isRemoteConnection;
    private bool _isEmbeddedReplica;

    private static readonly StateChangeEventArgs FromClosedToOpenEventArgs =
        new(ConnectionState.Closed, ConnectionState.Open);

    private static readonly StateChangeEventArgs FromOpenToClosedEventArgs =
        new(ConnectionState.Open, ConnectionState.Closed);

    public LibSqlConnection()
    {
    }

    public LibSqlConnection(string connectionString)
        => ConnectionString = connectionString;

    [AllowNull]
    public override string ConnectionString
    {
        get => _connectionString;
        set
        {
            EnsureConnectionClosed();
            _connectionString = value ?? string.Empty;
            _connectionStringBuilder = null;
        }
    }

    public override void ChangeDatabase(string databaseName)
        => throw new NotSupportedException("libSQL does not support changing databases on an open connection.");

    public override string Database => ConnectionStringBuilder.DataSource ?? string.Empty;

    public override string DataSource => ConnectionStringBuilder.DataSource ?? string.Empty;

    public override string ServerVersion => LibSqlVersion.GetVersionInfo();

    public override ConnectionState State => _connectionState;

    /// <summary>Gets or sets whether to enable statement caching for improved performance.</summary>
    public bool EnableStatementCaching
    {
        get => _enableStatementCaching;
        set
        {
            _enableStatementCaching = value;
            if (!value)
            {
                _statementCache?.Clear();
            }
        }
    }

    /// <summary>Gets or sets the maximum number of statements to cache. Takes effect on next Open.</summary>
    public int MaxCachedStatements { get; set; } = 100;

    internal LibSqlStatementCache? StatementCache => _statementCache;

    internal LibSqlTransaction? CurrentTransaction { get; set; }

    private LibSqlConnectionStringBuilder ConnectionStringBuilder
        => _connectionStringBuilder ??= new LibSqlConnectionStringBuilder(_connectionString);

    /// <summary>
    /// Closes <paramref name="connection"/> if it is still open and waits for native finalizers.
    /// Prefer this over <see cref="ClearAllPools"/> when other connections may be in use
    /// (e.g. parallel tests).
    /// </summary>
    public static void ClearPool(LibSqlConnection connection)
    {
        ArgumentNullException.ThrowIfNull(connection);

        try
        {
            connection.Close();
        }
        catch
        {
            // Best-effort.
        }

        OpenConnections.TryRemove(connection, out _);
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    /// <summary>Closes all open <see cref="LibSqlConnection"/> instances in this process.</summary>
    public static void ClearAllPools()
    {
        foreach (var connection in OpenConnections.Keys)
        {
            try
            {
                connection.Close();
            }
            catch
            {
                // Best-effort: continue clearing remaining connections.
            }
        }

        OpenConnections.Clear();
        GC.Collect();
        GC.WaitForPendingFinalizers();
    }

    public override void Open()
        => OpenAsync(CancellationToken.None).GetAwaiter().GetResult();

    public override async Task OpenAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        // Take the lock only around state checks / local open; await remote outside.
        LibSqlConnectionStringBuilder builder;
        lock (_lockObject)
        {
            EnsureConnectionClosed();
            builder = ConnectionStringBuilder;

            if (builder.Mode == LibSqlConnectionMode.EmbeddedReplica)
            {
                try
                {
                    OpenEmbeddedReplica(builder);
                }
                catch
                {
                    _connectionHandle?.Dispose();
                    _connectionHandle = null;
                    _databaseHandle?.Dispose();
                    _databaseHandle = null;
                    _isEmbeddedReplica = false;
                    throw;
                }

                return;
            }

            if (builder.Mode != LibSqlConnectionMode.Remote)
            {
                try
                {
                    OpenLocal(builder);
                }
                catch
                {
                    _connectionHandle?.Dispose();
                    _connectionHandle = null;
                    _databaseHandle?.Dispose();
                    _databaseHandle = null;
                    throw;
                }

                return;
            }
        }

        try
        {
            await OpenRemoteAsync(builder, cancellationToken).ConfigureAwait(false);
        }
        catch
        {
            lock (_lockObject)
            {
                _hranaSession?.Dispose();
                _hranaSession = null;
                _isRemoteConnection = false;
                _connectionState = ConnectionState.Closed;
            }

            throw;
        }
    }

    /// <summary>
    /// Synchronizes this embedded replica with its remote primary
    /// (<c>libsql_sync2</c>). Consistency matches native libSQL settings
    /// (<see cref="LibSqlConnectionStringBuilder.ReadYourWrites"/>, sync interval).
    /// </summary>
    public LibSqlSyncResult Sync()
    {
        EnsureConnectionOpen();

        if (!_isEmbeddedReplica || _databaseHandle is null || _databaseHandle.IsInvalid)
        {
            throw new InvalidOperationException(
                "Sync is only supported on open embedded-replica connections "
                + "(Data Source=<local path>;Sync URL=<primary>).");
        }

        using var activity = LibSqlActivitySource.StartSync();

        // Do not hold _lockObject across native sync — libsql uses an internal Tokio
        // runtime (block_on); nested locks with command execution are unsafe.
        var databaseHandle = _databaseHandle;
        var result = LibSqlNative.libsql_sync2(databaseHandle, out var replicated, out var errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            activity?.SetStatus(ActivityStatusCode.Error, errorMessage);
            throw new LibSqlException(
                $"Failed to sync embedded replica: {errorMessage}",
                result);
        }

        activity?.SetTag("libsql.sync.frames", replicated.FramesSynced);
        return new LibSqlSyncResult(replicated.FrameNo, replicated.FramesSynced);
    }

    /// <summary>
    /// Asynchronously synchronizes this embedded replica with its remote primary.
    /// Native sync is blocking; this offloads to the thread pool.
    /// </summary>
    public Task<LibSqlSyncResult> SyncAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.Run(Sync, cancellationToken);
    }

    public override void Close()
    {
        lock (_lockObject)
        {
            if (_connectionState == ConnectionState.Closed)
            {
                return;
            }

            try
            {
                // Finalize live commands/prepared stmts before native close
                // (busy SQLite close leaves the file locked with no managed handle left).
                DisposeTrackedCommandsNoLock();

                if (CurrentTransaction is { IsCompleted: false })
                {
                    try
                    {
                        CurrentTransaction.Rollback();
                    }
                    catch
                    {
                        // Suppress exceptions during cleanup.
                    }
                }

                CurrentTransaction = null;

                _statementCache?.Dispose();
                _statementCache = null;

                if (_isRemoteConnection)
                {
                    _hranaSession?.Dispose();
                    _hranaSession = null;
                    _isRemoteConnection = false;
                }
                else
                {
                    // Reset the native connection before disconnect so pending query/statement
                    // state cannot leave the database file locked across Close→Open cycles
                    // (EF HasTables / DatabaseCreator.Create open+close around probes).
                    if (_connectionHandle is { IsInvalid: false })
                    {
                        var resetResult = LibSqlNative.libsql_reset(_connectionHandle, out var resetErrorMsg);
                        if (resetErrorMsg != IntPtr.Zero)
                        {
                            LibSqlNative.libsql_free_error_msg(resetErrorMsg);
                        }

                        _ = resetResult;
                    }

                    _connectionHandle?.Dispose();
                    _connectionHandle = null;

                    _databaseHandle?.Dispose();
                    _databaseHandle = null;
                }

                _isEmbeddedReplica = false;
                _connectionState = ConnectionState.Closed;
                OpenConnections.TryRemove(this, out _);
                OnStateChange(FromOpenToClosedEventArgs);
            }
            catch
            {
                _connectionState = ConnectionState.Closed;
                OpenConnections.TryRemove(this, out _);
                throw;
            }
        }
    }

    internal void AddCommand(LibSqlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (Monitor.IsEntered(_lockObject))
        {
            PruneCommandReferencesNoLock();
            _commands.Add(new WeakReference<LibSqlCommand>(command));
            return;
        }

        lock (_lockObject)
        {
            PruneCommandReferencesNoLock();
            _commands.Add(new WeakReference<LibSqlCommand>(command));
        }
    }

    internal void RemoveCommand(LibSqlCommand command)
    {
        ArgumentNullException.ThrowIfNull(command);

        if (Monitor.IsEntered(_lockObject))
        {
            RemoveCommandNoLock(command);
            return;
        }

        lock (_lockObject)
        {
            RemoveCommandNoLock(command);
        }
    }

    private void RemoveCommandNoLock(LibSqlCommand command)
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            if (!_commands[i].TryGetTarget(out var target) || ReferenceEquals(target, command))
            {
                _commands.RemoveAt(i);
            }
        }
    }

    private void DisposeTrackedCommandsNoLock()
    {
        if (_commands.Count == 0)
        {
            return;
        }

        var snapshot = new List<LibSqlCommand>(_commands.Count);
        foreach (var weak in _commands)
        {
            if (weak.TryGetTarget(out var command))
            {
                snapshot.Add(command);
            }
        }

        _commands.Clear();

        foreach (var command in snapshot)
        {
            try
            {
                command.Dispose();
            }
            catch
            {
                // Suppress cleanup failures so Close can still release native handles.
            }
        }
    }

    private void PruneCommandReferencesNoLock()
    {
        for (var i = _commands.Count - 1; i >= 0; i--)
        {
            if (!_commands[i].TryGetTarget(out _))
            {
                _commands.RemoveAt(i);
            }
        }
    }

    protected override DbCommand CreateDbCommand()
        => CreateCommand();

    public new LibSqlCommand CreateCommand()
        => new() { Connection = this };

    protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        => BeginTransaction(isolationLevel);

    public new LibSqlTransaction BeginTransaction(IsolationLevel isolationLevel = IsolationLevel.Serializable)
        => BeginTransaction(isolationLevel, LibSqlTransactionBehavior.Deferred);

    public LibSqlTransaction BeginTransaction(IsolationLevel isolationLevel, LibSqlTransactionBehavior behavior)
    {
        ValidateIsolationLevel(isolationLevel);

        EnsureConnectionOpen();

        if (CurrentTransaction != null)
        {
            throw new InvalidOperationException(
                "A transaction is already active on this connection. Nested transactions are not supported.");
        }

        var transaction = new LibSqlTransaction(this, isolationLevel, behavior);

        try
        {
            if (isolationLevel == IsolationLevel.ReadUncommitted)
            {
                ExecutePragmaNoLock("PRAGMA read_uncommitted = 1;");
            }

            var beginStatement = transaction.GetBeginStatement();
            if (_isRemoteConnection)
            {
                using var command = CreateCommand();
                command.CommandText = beginStatement;
                command.ExecuteNonQuery();
            }
            else
            {
                var result = LibSqlNative.libsql_execute(_connectionHandle!, beginStatement, out var errorMsg);

                if (result != 0)
                {
                    var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
                    LibSqlNative.libsql_free_error_msg(errorMsg);
                    throw LibSqlException.FromErrorCode(result, $"Failed to begin transaction: {errorMessage}", beginStatement);
                }
            }

            CurrentTransaction = transaction;
            return transaction;
        }
        catch
        {
            if (isolationLevel == IsolationLevel.ReadUncommitted)
            {
                try
                {
                    ExecutePragmaNoLock("PRAGMA read_uncommitted = 0;");
                }
                catch
                {
                    // Best-effort restore.
                }
            }

            transaction.Dispose();
            throw;
        }
    }

    private void ExecutePragmaNoLock(string pragmaSql)
    {
        if (_isRemoteConnection)
        {
            using var command = CreateCommand();
            command.CommandText = pragmaSql;
            command.ExecuteNonQuery();
            return;
        }

        var result = LibSqlNative.libsql_execute(_connectionHandle!, pragmaSql, out var errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw LibSqlException.FromErrorCode(result, $"Failed to execute {pragmaSql}: {errorMessage}", pragmaSql);
        }
    }

    internal void ClearReadUncommittedIfNeeded(IsolationLevel isolationLevel)
    {
        if (isolationLevel != IsolationLevel.ReadUncommitted || _connectionState != ConnectionState.Open)
        {
            return;
        }

        try
        {
            ExecutePragmaNoLock("PRAGMA read_uncommitted = 0;");
        }
        catch
        {
            // Best-effort restore after commit/rollback/dispose.
        }
    }

    private static void ValidateIsolationLevel(IsolationLevel isolationLevel)
    {
        switch (isolationLevel)
        {
            case IsolationLevel.Serializable:
            case IsolationLevel.ReadUncommitted:
            case IsolationLevel.Unspecified:
                break;
            case IsolationLevel.ReadCommitted:
                throw new NotSupportedException("ReadCommitted isolation level is not supported by libSQL. Use Serializable instead.");
            case IsolationLevel.RepeatableRead:
                throw new NotSupportedException("RepeatableRead isolation level is not supported by libSQL. Use Serializable instead.");
            case IsolationLevel.Snapshot:
                throw new NotSupportedException("Snapshot isolation level is not supported by libSQL. Use Serializable instead.");
            case IsolationLevel.Chaos:
                throw new NotSupportedException("Chaos isolation level is not supported by libSQL.");
            default:
                throw new NotSupportedException($"Isolation level {isolationLevel} is not supported by libSQL.");
        }
    }

    internal LibSqlDatabaseHandle? DatabaseHandle => _databaseHandle;

    internal LibSqlConnectionHandle? ConnectionHandle => _connectionHandle;

    internal LibSqlConnectionHandle Handle
        => _connectionHandle ?? throw new InvalidOperationException("Connection is not open.");

    internal bool IsRemoteConnection => _isRemoteConnection;

    /// <summary>True when this connection was opened as an embedded replica.</summary>
    public bool IsEmbeddedReplica => _isEmbeddedReplica;

    internal ILibSqlHranaSession? HranaSession => _hranaSession;

    private void OpenLocal(LibSqlConnectionStringBuilder builder)
    {
        LibSqlNative.Initialize();

        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new LibSqlConnectionException(
                "Data source is required.",
                LibSqlConnectionStringBuilder.Redact(_connectionString));
        }

        int result;
        IntPtr dbHandle;
        IntPtr errorMsg;

        if (!string.IsNullOrEmpty(builder.EncryptionKey))
        {
            var config = new LibSqlConfig
            {
                DbPath = Marshal.StringToCoTaskMemUTF8(dataSource),
                PrimaryUrl = IntPtr.Zero,
                AuthToken = IntPtr.Zero,
                ReadYourWrites = 0,
                EncryptionKey = Marshal.StringToCoTaskMemUTF8(builder.EncryptionKey),
                SyncInterval = 0,
                WithWebpki = 0
            };

            try
            {
                result = LibSqlNative.libsql_open_sync_with_config(in config, out dbHandle, out errorMsg);
            }
            finally
            {
                FreeConfigStrings(ref config);
            }
        }
        else
        {
            result = LibSqlNative.libsql_open_file(dataSource, out dbHandle, out errorMsg);
        }

        CompleteNativeOpen(result, dbHandle, errorMsg, embeddedReplica: false);
    }

    private void OpenEmbeddedReplica(LibSqlConnectionStringBuilder builder)
    {
        LibSqlNative.Initialize();

        var dataSource = builder.DataSource;
        if (string.IsNullOrWhiteSpace(dataSource))
        {
            throw new LibSqlConnectionException(
                "Data source (local replica path) is required for embedded replica mode.",
                LibSqlConnectionStringBuilder.Redact(_connectionString));
        }

        if (dataSource.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("libsql://", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("ws://", StringComparison.OrdinalIgnoreCase)
            || dataSource.StartsWith("wss://", StringComparison.OrdinalIgnoreCase)
            || dataSource.Equals(":memory:", StringComparison.OrdinalIgnoreCase))
        {
            throw new LibSqlConnectionException(
                "Embedded replica Data Source must be a local file path "
                + "(not :memory: or a remote URL). Use Sync URL for the primary.",
                LibSqlConnectionStringBuilder.Redact(_connectionString));
        }

        var syncUrl = builder.SyncUrl;
        if (string.IsNullOrWhiteSpace(syncUrl))
        {
            throw new LibSqlConnectionException(
                "Sync URL is required for embedded replica mode.",
                LibSqlConnectionStringBuilder.Redact(_connectionString));
        }

        var primaryUrl = LibSqlRemoteTransport.NormalizeLibSqlToHttpUrl(builder.SyncUrl!.Trim());
        if (builder.Offline
            && primaryUrl.IndexOf("offline", StringComparison.OrdinalIgnoreCase) < 0)
        {
            primaryUrl += primaryUrl.Contains('?', StringComparison.Ordinal) ? "&offline" : "?offline";
        }

        var useWebpki = primaryUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase);
        var authToken = builder.EffectiveSyncAuthToken ?? string.Empty;
        var directory = Path.GetDirectoryName(Path.GetFullPath(dataSource));
        if (!string.IsNullOrEmpty(directory))
        {
            Directory.CreateDirectory(directory);
        }

        int result;
        IntPtr dbHandle;
        IntPtr errorMsg;

        // Prefer the dedicated webpki entry point when no advanced config knobs are set —
        // it avoids libsql_config layout/padding risk across native versions.
        // Native sync_interval is seconds (see libsql C bindings).
        var useSimpleWebpki =
            useWebpki
            && builder.SyncInterval == 0
            && string.IsNullOrEmpty(builder.EncryptionKey);

        if (useSimpleWebpki)
        {
            result = LibSqlNative.libsql_open_sync_with_webpki(
                dataSource,
                primaryUrl,
                authToken,
                builder.ReadYourWrites ? (byte)1 : (byte)0,
                encryptionKey: null,
                out dbHandle,
                out errorMsg);
        }
        else
        {
            var config = new LibSqlConfig
            {
                DbPath = Marshal.StringToCoTaskMemUTF8(dataSource),
                PrimaryUrl = Marshal.StringToCoTaskMemUTF8(primaryUrl),
                AuthToken = Marshal.StringToCoTaskMemUTF8(authToken),
                ReadYourWrites = builder.ReadYourWrites ? (byte)1 : (byte)0,
                EncryptionKey = string.IsNullOrEmpty(builder.EncryptionKey)
                    ? IntPtr.Zero
                    : Marshal.StringToCoTaskMemUTF8(builder.EncryptionKey),
                SyncInterval = builder.SyncInterval,
                WithWebpki = useWebpki ? (byte)1 : (byte)0
            };

            try
            {
                result = LibSqlNative.libsql_open_sync_with_config(in config, out dbHandle, out errorMsg);
            }
            finally
            {
                FreeConfigStrings(ref config);
            }
        }

        CompleteNativeOpen(result, dbHandle, errorMsg, embeddedReplica: true);
    }

    private void CompleteNativeOpen(int result, IntPtr dbHandle, IntPtr errorMsg, bool embeddedReplica)
    {
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            throw new LibSqlConnectionException(
                $"Failed to open database: {errorMessage}",
                result,
                LibSqlConnectionStringBuilder.Redact(_connectionString));
        }

        _databaseHandle = new LibSqlDatabaseHandle(dbHandle);

        result = LibSqlNative.libsql_connect(_databaseHandle, out var connHandle, out errorMsg);
        if (result != 0)
        {
            var errorMessage = LibSqlHelper.GetErrorMessage(errorMsg);
            LibSqlNative.libsql_free_error_msg(errorMsg);
            _databaseHandle.Dispose();
            _databaseHandle = null;
            throw new LibSqlConnectionException(
                $"Failed to connect to database: {errorMessage}",
                result,
                LibSqlConnectionStringBuilder.Redact(_connectionString));
        }

        _connectionHandle = new LibSqlConnectionHandle(connHandle);
        _isEmbeddedReplica = embeddedReplica;
        _connectionState = ConnectionState.Open;
        OpenConnections.TryAdd(this, 0);

        if (_enableStatementCaching)
        {
            _statementCache = new LibSqlStatementCache(MaxCachedStatements);
        }

        OnStateChange(FromClosedToOpenEventArgs);
    }

    private static void FreeConfigStrings(ref LibSqlConfig config)
    {
        if (config.DbPath != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(config.DbPath);
            config.DbPath = IntPtr.Zero;
        }

        if (config.PrimaryUrl != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(config.PrimaryUrl);
            config.PrimaryUrl = IntPtr.Zero;
        }

        if (config.AuthToken != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(config.AuthToken);
            config.AuthToken = IntPtr.Zero;
        }

        if (config.EncryptionKey != IntPtr.Zero)
        {
            Marshal.FreeCoTaskMem(config.EncryptionKey);
            config.EncryptionKey = IntPtr.Zero;
        }
    }

    private async Task OpenRemoteAsync(LibSqlConnectionStringBuilder builder, CancellationToken cancellationToken)
    {
        var dataSource = builder.DataSource
            ?? throw new LibSqlConnectionException(
                "Data source is required.",
                LibSqlConnectionStringBuilder.Redact(_connectionString));

        try
        {
            cancellationToken.ThrowIfCancellationRequested();

            if (LibSqlRemoteTransport.IsWebSocketUrl(dataSource))
            {
                _hranaSession = await LibSqlWsClient
                    .ConnectAsync(dataSource, builder.AuthToken, cancellationToken)
                    .ConfigureAwait(false);
            }
            else
            {
                _hranaSession = new LibSqlHttpClient(dataSource, builder.AuthToken);
            }

            using var timeoutCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            timeoutCts.CancelAfter(TimeSpan.FromSeconds(15));
            var ok = await _hranaSession.TestConnectionAsync(timeoutCts.Token).ConfigureAwait(false);
            if (!ok)
            {
                throw new LibSqlConnectionException(
                    "Failed to connect to remote libSQL server",
                    0,
                    LibSqlConnectionStringBuilder.Redact(_connectionString));
            }
        }
        catch (OperationCanceledException)
        {
            _hranaSession?.Dispose();
            _hranaSession = null;
            throw;
        }
        catch (Exception ex) when (ex is not LibSqlConnectionException)
        {
            _hranaSession?.Dispose();
            _hranaSession = null;
            throw new LibSqlConnectionException(
                "Failed to connect to remote libSQL server",
                LibSqlConnectionStringBuilder.Redact(_connectionString),
                ex is AggregateException { InnerException: { } inner } ? inner : ex);
        }

        lock (_lockObject)
        {
            _isRemoteConnection = true;
            _connectionState = ConnectionState.Open;
            OpenConnections.TryAdd(this, 0);
        }

        OnStateChange(FromClosedToOpenEventArgs);
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing && _connectionState == ConnectionState.Open)
        {
            Close();
        }

        base.Dispose(disposing);
    }

    private void EnsureConnectionClosed([CallerMemberName] string operation = "")
    {
        if (_connectionState != ConnectionState.Closed)
        {
            throw new InvalidOperationException($"{operation} requires a closed connection.");
        }
    }

    private void EnsureConnectionOpen([CallerMemberName] string operation = "")
    {
        if (_connectionState != ConnectionState.Open)
        {
            throw new InvalidOperationException($"{operation} requires an open connection.");
        }
    }
}
