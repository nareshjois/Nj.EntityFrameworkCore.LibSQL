using System.Data;
using System.Data.Common;
using Nj.LibSql.Bindings;
using Nj.LibSql.Data.Exceptions;

namespace Nj.LibSql.Data;

/// <summary>Represents a transaction to be performed at a libSQL database.</summary>
public sealed class LibSqlTransaction : DbTransaction
{
    private LibSqlConnection? _connection;
    private readonly IsolationLevel _isolationLevel;
    private readonly LibSqlTransactionBehavior _behavior;
    private bool _completed;
    private bool _disposed;

    internal LibSqlTransaction(
        LibSqlConnection connection,
        IsolationLevel isolationLevel,
        LibSqlTransactionBehavior behavior = LibSqlTransactionBehavior.Deferred)
    {
        _connection = connection ?? throw new ArgumentNullException(nameof(connection));
        _isolationLevel = isolationLevel == IsolationLevel.Unspecified ? IsolationLevel.Serializable : isolationLevel;
        _behavior = behavior;
    }

    public new LibSqlConnection? Connection => _connection;

    protected override DbConnection? DbConnection => _connection;

    public override IsolationLevel IsolationLevel => _isolationLevel;

    /// <summary>Gets the transaction behavior for this transaction.</summary>
    public LibSqlTransactionBehavior Behavior => _behavior;

    /// <summary>Gets a value indicating whether the transaction has been committed or rolled back.</summary>
    public bool IsCompleted => _completed;

    public override void Commit()
    {
        ValidateTransaction();

        try
        {
            if (_connection!.IsRemoteConnection)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "COMMIT";
                command.ExecuteNonQuery();
            }
            else
            {
                var result = LibSqlNative.libsql_execute(_connection.Handle, "COMMIT", out var errorMessage);
                if (result != 0)
                {
                    var errorMsg = LibSqlHelper.GetErrorMessage(errorMessage);
                    LibSqlNative.libsql_free_error_msg(errorMessage);
                    throw new LibSqlException($"Failed to commit transaction: {errorMsg}");
                }
            }

            _completed = true;
            _connection.CurrentTransaction = null;
            _connection.ClearReadUncommittedIfNeeded(_isolationLevel);
        }
        catch (Exception ex) when (ex is not LibSqlException)
        {
            throw new LibSqlException("An error occurred while committing the transaction.", ex);
        }
    }

    public override void Rollback()
    {
        ValidateTransaction();

        try
        {
            if (_connection!.IsRemoteConnection)
            {
                using var command = _connection.CreateCommand();
                command.CommandText = "ROLLBACK";
                command.ExecuteNonQuery();
            }
            else
            {
                var result = LibSqlNative.libsql_execute(_connection.Handle, "ROLLBACK", out var errorMessage);
                if (result != 0)
                {
                    var errorMsg = LibSqlHelper.GetErrorMessage(errorMessage);
                    LibSqlNative.libsql_free_error_msg(errorMessage);
                    throw new LibSqlException($"Failed to rollback transaction: {errorMsg}");
                }
            }

            _completed = true;
            _connection.CurrentTransaction = null;
            _connection.ClearReadUncommittedIfNeeded(_isolationLevel);
        }
        catch (Exception ex) when (ex is not LibSqlException)
        {
            throw new LibSqlException("An error occurred while rolling back the transaction.", ex);
        }
    }

    /// <summary>Validates that the transaction is in a valid state for operations.</summary>
    public void ValidateTransaction()
    {
        ObjectDisposedException.ThrowIf(_disposed, this);

        if (_completed)
        {
            throw new InvalidOperationException("The transaction has already been completed.");
        }

        if (_connection == null)
        {
            throw new InvalidOperationException("The transaction is not associated with a connection.");
        }

        if (_connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("The connection is not open.");
        }

        if (!ReferenceEquals(_connection.CurrentTransaction, this))
        {
            throw new InvalidOperationException("This transaction is not the current transaction for the connection.");
        }
    }

    /// <summary>Generates the appropriate BEGIN SQL statement based on isolation level and behavior.</summary>
    internal string GetBeginStatement()
        => _behavior switch
        {
            LibSqlTransactionBehavior.Deferred => "BEGIN DEFERRED",
            LibSqlTransactionBehavior.Immediate => "BEGIN IMMEDIATE",
            LibSqlTransactionBehavior.Exclusive => "BEGIN EXCLUSIVE",
            LibSqlTransactionBehavior.ReadOnly => "BEGIN READONLY",
            _ => "BEGIN"
        };

    protected override void Dispose(bool disposing)
    {
        if (_disposed)
        {
            return;
        }

        if (disposing && !_completed && _connection != null)
        {
            try
            {
                Rollback();
            }
            catch
            {
                _completed = true;
                _connection.CurrentTransaction = null;
                try
                {
                    _connection.ClearReadUncommittedIfNeeded(_isolationLevel);
                }
                catch
                {
                    // Best-effort.
                }
            }
        }

        _connection = null;
        _disposed = true;

        base.Dispose(disposing);
    }
}
