using System.Data;
using System.Data.Common;

namespace Nj.LibSql.Data;

/// <summary>A single command within a <see cref="LibSqlBatch"/>.</summary>
public sealed class LibSqlBatchCommand : DbBatchCommand
{
    private string _commandText = string.Empty;
    private CommandType _commandType = CommandType.Text;
    private readonly LibSqlParameterCollection _parameters = [];
    private int _recordsAffected = -1;

    /// <inheritdoc />
    public override string CommandText
    {
        get => _commandText;
        set => _commandText = value ?? string.Empty;
    }

    /// <inheritdoc />
    public override CommandType CommandType
    {
        get => _commandType;
        set
        {
            if (value != CommandType.Text)
            {
                throw new NotSupportedException(
                    $"CommandType.{value} is not supported for LibSqlBatchCommand.");
            }

            _commandType = value;
        }
    }

    /// <inheritdoc />
    public override int RecordsAffected => _recordsAffected;

    /// <inheritdoc />
    protected override DbParameterCollection DbParameterCollection => _parameters;

    /// <inheritdoc />
    public override bool CanCreateParameter => true;

    /// <inheritdoc />
    public override DbParameter CreateParameter()
        => new LibSqlParameter();

    internal LibSqlParameterCollection LibSqlParameters => _parameters;

    internal void SetRecordsAffected(int value)
        => _recordsAffected = value;
}

/// <summary>Collection of <see cref="LibSqlBatchCommand"/> instances.</summary>
public sealed class LibSqlBatchCommandCollection : DbBatchCommandCollection
{
    private readonly List<DbBatchCommand> _commands = [];

    /// <inheritdoc />
    public override int Count => _commands.Count;

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override void Add(DbBatchCommand item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _commands.Add(item);
    }

    /// <inheritdoc />
    public override void Clear()
        => _commands.Clear();

    /// <inheritdoc />
    public override bool Contains(DbBatchCommand item)
        => _commands.Contains(item);

    /// <inheritdoc />
    public override void CopyTo(DbBatchCommand[] array, int arrayIndex)
        => _commands.CopyTo(array, arrayIndex);

    /// <inheritdoc />
    public override IEnumerator<DbBatchCommand> GetEnumerator()
        => _commands.GetEnumerator();

    /// <inheritdoc />
    public override int IndexOf(DbBatchCommand item)
        => _commands.IndexOf(item);

    /// <inheritdoc />
    public override void Insert(int index, DbBatchCommand item)
    {
        ArgumentNullException.ThrowIfNull(item);
        _commands.Insert(index, item);
    }

    /// <inheritdoc />
    public override bool Remove(DbBatchCommand item)
        => _commands.Remove(item);

    /// <inheritdoc />
    public override void RemoveAt(int index)
        => _commands.RemoveAt(index);

    /// <inheritdoc />
    protected override DbBatchCommand GetBatchCommand(int index)
        => _commands[index];

    /// <inheritdoc />
    protected override void SetBatchCommand(int index, DbBatchCommand batchCommand)
        => _commands[index] = batchCommand ?? throw new ArgumentNullException(nameof(batchCommand));
}
