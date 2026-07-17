using System.Collections;
using System.Data.Common;
using Nj.LibSql.Data.Http;

namespace Nj.LibSql.Data;

/// <summary>
/// Multi-result <see cref="DbDataReader"/> over buffered Hrana query results from a remote batch.
/// </summary>
internal sealed class LibSqlBatchDataReader : DbDataReader
{
    private readonly IReadOnlyList<LibSqlHttpDataReader> _readers;
    private int _index;
    private bool _closed;

    public LibSqlBatchDataReader(IReadOnlyList<LibSqlHttpDataReader> readers)
    {
        ArgumentNullException.ThrowIfNull(readers);
        if (readers.Count == 0)
        {
            throw new ArgumentException("At least one result set is required.", nameof(readers));
        }

        _readers = readers;
    }

    private LibSqlHttpDataReader Current
    {
        get
        {
            ObjectDisposedException.ThrowIf(_closed, this);
            return _readers[_index];
        }
    }

    public override int FieldCount => Current.FieldCount;

    public override bool HasRows => Current.HasRows;

    public override bool IsClosed => _closed;

    public override int RecordsAffected => Current.RecordsAffected;

    public override int Depth => 0;

    public override object this[int ordinal] => Current[ordinal];

    public override object this[string name] => Current[name];

    public override bool Read()
        => Current.Read();

    public override bool NextResult()
    {
        ObjectDisposedException.ThrowIf(_closed, this);
        if (_index + 1 >= _readers.Count)
        {
            return false;
        }

        _index++;
        return true;
    }

    public override void Close()
    {
        if (_closed)
        {
            return;
        }

        foreach (var reader in _readers)
        {
            reader.Close();
        }

        _closed = true;
    }

    public override string GetName(int ordinal)
        => Current.GetName(ordinal);

    public override int GetOrdinal(string name)
        => Current.GetOrdinal(name);

    public override string GetDataTypeName(int ordinal)
        => Current.GetDataTypeName(ordinal);

    public override Type GetFieldType(int ordinal)
        => Current.GetFieldType(ordinal);

    public override object GetValue(int ordinal)
        => Current.GetValue(ordinal);

    public override int GetValues(object[] values)
        => Current.GetValues(values);

    public override bool IsDBNull(int ordinal)
        => Current.IsDBNull(ordinal);

    public override bool GetBoolean(int ordinal)
        => Current.GetBoolean(ordinal);

    public override byte GetByte(int ordinal)
        => Current.GetByte(ordinal);

    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        => Current.GetBytes(ordinal, dataOffset, buffer, bufferOffset, length);

    public override char GetChar(int ordinal)
        => Current.GetChar(ordinal);

    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        => Current.GetChars(ordinal, dataOffset, buffer, bufferOffset, length);

    public override DateTime GetDateTime(int ordinal)
        => Current.GetDateTime(ordinal);

    public override decimal GetDecimal(int ordinal)
        => Current.GetDecimal(ordinal);

    public override double GetDouble(int ordinal)
        => Current.GetDouble(ordinal);

    public override float GetFloat(int ordinal)
        => Current.GetFloat(ordinal);

    public override Guid GetGuid(int ordinal)
        => Current.GetGuid(ordinal);

    public override short GetInt16(int ordinal)
        => Current.GetInt16(ordinal);

    public override int GetInt32(int ordinal)
        => Current.GetInt32(ordinal);

    public override long GetInt64(int ordinal)
        => Current.GetInt64(ordinal);

    public override string GetString(int ordinal)
        => Current.GetString(ordinal);

    public override IEnumerator GetEnumerator()
        => new DbEnumerator(this);
}
