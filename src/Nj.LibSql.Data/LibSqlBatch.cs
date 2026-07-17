using System.Data;
using System.Data.Common;
using System.Diagnostics;
using System.Globalization;
using Nj.LibSql.Data.Exceptions;
using Nj.LibSql.Data.Http;
using Nj.LibSql.Data.Internal;

namespace Nj.LibSql.Data;

/// <summary>
/// Executes multiple SQL statements in one round-trip against a remote Hrana session,
/// or sequentially (non-query) against a local libSQL connection.
/// </summary>
public sealed class LibSqlBatch : DbBatch
{
    private readonly LibSqlBatchCommandCollection _commands = new();
    private LibSqlConnection? _connection;
    private LibSqlTransaction? _transaction;
    private int _timeout = 30;
    private CancellationTokenSource? _activeCts;

    /// <inheritdoc />
    protected override DbBatchCommandCollection DbBatchCommands => _commands;

    /// <inheritdoc />
    protected override DbConnection? DbConnection
    {
        get => _connection;
        set
        {
            if (value is null)
            {
                _connection = null;
                return;
            }

            _connection = value as LibSqlConnection
                ?? throw new InvalidOperationException(
                    $"Connection must be a {nameof(LibSqlConnection)}.");
        }
    }

    /// <inheritdoc />
    protected override DbTransaction? DbTransaction
    {
        get => _transaction;
        set
        {
            if (value is null)
            {
                _transaction = null;
                return;
            }

            _transaction = value as LibSqlTransaction
                ?? throw new InvalidOperationException(
                    $"Transaction must be a {nameof(LibSqlTransaction)}.");
        }
    }

    /// <inheritdoc />
    public override int Timeout
    {
        get => _timeout;
        set
        {
            ArgumentOutOfRangeException.ThrowIfNegative(value);
            _timeout = value;
        }
    }

    /// <inheritdoc />
    public new LibSqlBatchCommand CreateBatchCommand()
        => (LibSqlBatchCommand)CreateDbBatchCommand();

    /// <inheritdoc />
    protected override DbBatchCommand CreateDbBatchCommand()
        => new LibSqlBatchCommand();

    /// <inheritdoc />
    public override void Prepare()
    {
    }

    /// <inheritdoc />
    public override Task PrepareAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public override void Cancel()
        => _activeCts?.Cancel();

    /// <inheritdoc />
    public override int ExecuteNonQuery()
        => ExecuteNonQueryAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc />
    public override async Task<int> ExecuteNonQueryAsync(CancellationToken cancellationToken = default)
    {
        var connection = GetOpenConnection();
        if (connection.IsRemoteConnection)
        {
            var results = await ExecuteRemoteAsync(connection, cancellationToken).ConfigureAwait(false);
            var total = 0;
            for (var i = 0; i < results.Count; i++)
            {
                var affected = (int)results[i].AffectedRowCount;
                ((LibSqlBatchCommand)_commands[i]).SetRecordsAffected(affected);
                total += affected;
            }

            return total;
        }

        var totalLocal = 0;
        foreach (DbBatchCommand item in _commands)
        {
            var batchCommand = (LibSqlBatchCommand)item;
            await using var command = CreateBoundCommand(connection, batchCommand);
            var affected = await command.ExecuteNonQueryAsync(cancellationToken).ConfigureAwait(false);
            batchCommand.SetRecordsAffected(affected);
            totalLocal += affected;
        }

        return totalLocal;
    }

    /// <inheritdoc />
    public override object? ExecuteScalar()
        => ExecuteScalarAsync(CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc />
    public override async Task<object?> ExecuteScalarAsync(CancellationToken cancellationToken = default)
    {
        await using var reader = await ExecuteDbDataReaderAsync(CommandBehavior.Default, cancellationToken)
            .ConfigureAwait(false);
        if (await reader.ReadAsync(cancellationToken).ConfigureAwait(false) && reader.FieldCount > 0)
        {
            return reader.GetValue(0);
        }

        return null;
    }

    /// <inheritdoc />
    protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        => ExecuteDbDataReaderAsync(behavior, CancellationToken.None).GetAwaiter().GetResult();

    /// <inheritdoc />
    protected override async Task<DbDataReader> ExecuteDbDataReaderAsync(
        CommandBehavior behavior,
        CancellationToken cancellationToken)
    {
        var connection = GetOpenConnection();
        if (connection.IsRemoteConnection)
        {
            var results = await ExecuteRemoteAsync(connection, cancellationToken).ConfigureAwait(false);
            for (var i = 0; i < results.Count; i++)
            {
                ((LibSqlBatchCommand)_commands[i]).SetRecordsAffected((int)results[i].AffectedRowCount);
            }

            return new LibSqlBatchDataReader(results.Select(r => new LibSqlHttpDataReader(r)).ToList());
        }

        if (_commands.Count > 1)
        {
            throw new NotSupportedException(
                "Local LibSqlBatch.ExecuteReader supports a single batch command. "
                + "Use a remote connection for multi-command result batches, "
                + "or ExecuteNonQuery for sequential local statements.");
        }

        var batchCommand = (LibSqlBatchCommand)_commands[0];
        var command = CreateBoundCommand(connection, batchCommand);
        var reader = await command.ExecuteReaderAsync(behavior, cancellationToken).ConfigureAwait(false);
        batchCommand.SetRecordsAffected(reader.RecordsAffected);
        return reader;
    }

    private LibSqlConnection GetOpenConnection()
    {
        if (_connection is null)
        {
            throw new InvalidOperationException("Connection must be set before executing a batch.");
        }

        if (_connection.State != ConnectionState.Open)
        {
            throw new InvalidOperationException("Connection must be open before executing a batch.");
        }

        if (_commands.Count == 0)
        {
            throw new InvalidOperationException("Batch contains no commands.");
        }

        return _connection;
    }

    private LibSqlCommand CreateBoundCommand(LibSqlConnection connection, LibSqlBatchCommand batchCommand)
    {
        var command = connection.CreateCommand();
        command.CommandText = batchCommand.CommandText;
        command.Transaction = _transaction;
        command.CommandTimeout = _timeout;
        foreach (LibSqlParameter parameter in batchCommand.LibSqlParameters)
        {
            command.Parameters.Add(new LibSqlParameter
            {
                ParameterName = parameter.ParameterName,
                Value = parameter.Value,
                DbType = parameter.DbType,
                Size = parameter.Size,
                Direction = parameter.Direction,
                IsNullable = parameter.IsNullable,
                SourceColumn = parameter.SourceColumn,
            });
        }

        return command;
    }

    private async Task<List<HranaQueryResult>> ExecuteRemoteAsync(
        LibSqlConnection connection,
        CancellationToken cancellationToken)
    {
        var session = connection.HranaSession
            ?? throw new InvalidOperationException("Remote Hrana session is not available.");

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        if (_timeout > 0)
        {
            cts.CancelAfter(TimeSpan.FromSeconds(_timeout));
        }

        _activeCts = cts;
        try
        {
            using var activity = LibSqlActivitySource.StartRemoteCommand("ExecuteBatch", $"batch({_commands.Count})");
            try
            {
                var request = new HranaBatchRequest();
                foreach (DbBatchCommand item in _commands)
                {
                    var batchCommand = (LibSqlBatchCommand)item;
                    if (string.IsNullOrWhiteSpace(batchCommand.CommandText))
                    {
                        throw new InvalidOperationException("Batch command CommandText must be specified.");
                    }

                    var sql = batchCommand.CommandText.Trim();
                    var layout = SqlParameterLayout.Parse(sql);
                    request.Requests.Add(new HranaRequest
                    {
                        Type = HranaTypes.Execute,
                        Statement = new HranaStatement
                        {
                            Sql = layout.ToIndexedParameterSql(sql),
                            Args = CreateArgs(layout, batchCommand.LibSqlParameters),
                        },
                    });
                }

                var response = await session.ExecuteBatchAsync(request, cts.Token).ConfigureAwait(false);
                if (response.Results.Count != _commands.Count)
                {
                    throw new LibSqlException(
                        $"Expected {_commands.Count} batch results but received {response.Results.Count}.");
                }

                var queryResults = new List<HranaQueryResult>(_commands.Count);
                for (var i = 0; i < response.Results.Count; i++)
                {
                    var result = response.Results[i];
                    if (result.Type == HranaTypes.Error || result.Response?.Type == HranaTypes.Error)
                    {
                        var message = result.Error?.Message
                            ?? result.Response?.Error?.Message
                            ?? "Unknown server error";
                        throw new LibSqlException($"SQL Error: {message}");
                    }

                    var queryResult = result.Response?.Result
                        ?? throw new LibSqlException("Batch step returned no result payload.");
                    queryResults.Add(queryResult);
                }

                return queryResults;
            }
            catch (Exception ex)
            {
                activity?.SetStatus(ActivityStatusCode.Error, ex.Message);
                throw;
            }
        }
        finally
        {
            if (ReferenceEquals(_activeCts, cts))
            {
                _activeCts = null;
            }
        }
    }

    private static List<HranaValue>? CreateArgs(SqlParameterLayout layout, LibSqlParameterCollection parameters)
    {
        if (parameters.Count == 0)
        {
            return null;
        }

        var bindings = layout.ResolveBindings(parameters);
        if (bindings.Count == 0)
        {
            return null;
        }

        var args = Enumerable
            .Range(0, layout.MaxPosition)
            .Select(_ => new HranaValue { Type = HranaTypes.Null, Value = null })
            .ToList();

        foreach (var binding in bindings)
        {
            args[binding.Position - 1] = ConvertParameter(binding.Parameter);
        }

        return args;
    }

    private static HranaValue ConvertParameter(LibSqlParameter parameter)
    {
        var value = parameter.Value;
        if (value is null || value is DBNull)
        {
            return new HranaValue { Type = HranaTypes.Null, Value = null };
        }

        return parameter.DbType switch
        {
            DbType.Boolean => new HranaValue
            {
                Type = HranaTypes.Integer,
                Value = (Convert.ToBoolean(value, CultureInfo.InvariantCulture) ? 1 : 0)
                    .ToString(CultureInfo.InvariantCulture),
            },
            DbType.Byte or DbType.Int16 or DbType.Int32 or DbType.Int64 or DbType.SByte or DbType.UInt16
                or DbType.UInt32 or DbType.UInt64 => new HranaValue
                {
                    Type = HranaTypes.Integer,
                    Value = Convert.ToInt64(value, CultureInfo.InvariantCulture)
                        .ToString(CultureInfo.InvariantCulture),
                },
            DbType.Single or DbType.Double or DbType.Decimal or DbType.Currency => new HranaValue
            {
                Type = HranaTypes.Float,
                Value = Convert.ToDouble(value, CultureInfo.InvariantCulture),
            },
            DbType.Binary => new HranaValue
            {
                Type = HranaTypes.Blob,
                Base64 = Convert.ToBase64String(
                    value as byte[]
                    ?? throw new InvalidOperationException("Binary parameter must be byte[].")),
            },
            _ => new HranaValue
            {
                Type = HranaTypes.Text,
                Value = Convert.ToString(value, CultureInfo.InvariantCulture),
            },
        };
    }
}
