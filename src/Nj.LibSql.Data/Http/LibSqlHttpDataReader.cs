#nullable disable warnings

using System;
using System.Collections;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.Json;
using Nj.LibSql.Data.Exceptions;

namespace Nj.LibSql.Data.Http;

/// <summary>
/// Data reader for HTTP-based libSQL connections.
/// </summary>
internal sealed class LibSqlHttpDataReader : DbDataReader
{
    private readonly HranaQueryResult _result;
    private readonly List<HranaColumn> _columns;
    private readonly List<List<HranaValue>> _rows;
    private int _currentRowIndex = -1;
    private bool _isClosed;

    /// <summary>Initializes a new instance of the <see cref="LibSqlHttpDataReader"/> class.</summary>
    public LibSqlHttpDataReader(HranaQueryResult result)
    {
        ArgumentNullException.ThrowIfNull(result);
        _result = result;
        _columns = result.Cols ?? new List<HranaColumn>();
        _rows = result.Rows ?? new List<List<HranaValue>>();
    }

    /// <inheritdoc />
    public override int FieldCount => _columns.Count;

    /// <inheritdoc />
    public override bool HasRows => _rows.Count > 0;

    /// <inheritdoc />
    public override bool IsClosed => _isClosed;

    /// <inheritdoc />
    public override int RecordsAffected => (int)_result.AffectedRowCount;

    /// <inheritdoc />
    public override object this[int ordinal] => GetValue(ordinal);

    /// <inheritdoc />
    public override object this[string name] => GetValue(GetOrdinal(name));

    /// <inheritdoc />
    public override int Depth => 0;

    /// <inheritdoc />
    public override bool Read()
    {
        if (_isClosed)
            throw new InvalidOperationException("DataReader is closed");

        _currentRowIndex++;
        return _currentRowIndex < _rows.Count;
    }

    /// <inheritdoc />
    public override bool NextResult()
    {
        // HTTP connections don't support multiple result sets
        return false;
    }

    /// <inheritdoc />
    public override void Close()
    {
        _isClosed = true;
    }

    /// <inheritdoc />
    public override string GetName(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _columns.Count)
            throw new ArgumentOutOfRangeException(nameof(ordinal));

        return _columns[ordinal].Name ?? $"Column{ordinal}";
    }

    /// <inheritdoc />
    public override int GetOrdinal(string name)
    {
        if (string.IsNullOrEmpty(name))
            throw new ArgumentException("Name cannot be null or empty", nameof(name));

        for (int i = 0; i < _columns.Count; i++)
        {
            if (string.Equals(_columns[i].Name, name, StringComparison.OrdinalIgnoreCase))
                return i;
        }

        throw new ArgumentException($"Column '{name}' not found", nameof(name));
    }

    /// <inheritdoc />
    public override string GetDataTypeName(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _columns.Count)
            throw new ArgumentOutOfRangeException(nameof(ordinal));

        return _columns[ordinal].DeclType ?? "TEXT";
    }

    /// <inheritdoc />
    [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
    public override Type GetFieldType(int ordinal)
    {
        if (ordinal < 0 || ordinal >= _columns.Count)
            throw new ArgumentOutOfRangeException(nameof(ordinal));

        var declType = _columns[ordinal].DeclType?.ToUpperInvariant();
        return declType switch
        {
            string dt when dt.Contains("INT", StringComparison.OrdinalIgnoreCase) => typeof(long),
            string dt when dt.Contains("REAL", StringComparison.OrdinalIgnoreCase) || dt.Contains("FLOAT", StringComparison.OrdinalIgnoreCase) || dt.Contains("DOUBLE", StringComparison.OrdinalIgnoreCase) => typeof(double),
            string dt when dt.Contains("BLOB", StringComparison.OrdinalIgnoreCase) => typeof(byte[]),
            _ => typeof(string)
        };
    }

    /// <inheritdoc />
    public override object GetValue(int ordinal)
    {
        if (_isClosed)
            throw new InvalidOperationException("DataReader is closed");

        if (_currentRowIndex < 0 || _currentRowIndex >= _rows.Count)
            throw new InvalidOperationException("No current row");

        if (ordinal < 0 || ordinal >= _columns.Count)
            throw new ArgumentOutOfRangeException(nameof(ordinal));

        var row = _rows[_currentRowIndex];
        if (ordinal >= row.Count)
            return DBNull.Value;

        var value = row[ordinal];
        if (value == null || value.Type == HranaTypes.Null)
            return DBNull.Value;

        return ConvertValue(value);
    }

    /// <inheritdoc />
    public override int GetValues(object[] values)
    {
        ArgumentNullException.ThrowIfNull(values);

        var count = Math.Min(values.Length, FieldCount);
        for (int i = 0; i < count; i++)
        {
            values[i] = GetValue(i);
        }
        return count;
    }

    /// <inheritdoc />
    public override bool IsDBNull(int ordinal)
    {
        if (_isClosed)
            throw new InvalidOperationException("DataReader is closed");

        if (_currentRowIndex < 0 || _currentRowIndex >= _rows.Count)
            throw new InvalidOperationException("No current row");

        if (ordinal < 0 || ordinal >= _columns.Count)
            throw new ArgumentOutOfRangeException(nameof(ordinal));

        var row = _rows[_currentRowIndex];
        if (ordinal >= row.Count)
            return true;

        var value = row[ordinal];
        return value == null || value.Type == HranaTypes.Null;
    }

    /// <inheritdoc />
    public override bool GetBoolean(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to boolean");

        return Convert.ToBoolean(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override byte GetByte(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to byte");

        return Convert.ToByte(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            return 0;

        if (value is not byte[] bytes)
            throw new InvalidCastException("Value is not a byte array");

        if (buffer == null)
            return bytes.Length;

        var bytesToCopy = Math.Min(length, bytes.Length - (int)dataOffset);
        if (bytesToCopy <= 0)
            return 0;

        Array.Copy(bytes, dataOffset, buffer, bufferOffset, bytesToCopy);
        return bytesToCopy;
    }

    /// <inheritdoc />
    public override char GetChar(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to char");

        return Convert.ToChar(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            return 0;

        var str = value.ToString();
        if (str == null)
            return 0;

        if (buffer == null)
            return str.Length;

        var charsToRead = Math.Min(length, str.Length - (int)dataOffset);
        if (charsToRead <= 0)
            return 0;

        str.CopyTo((int)dataOffset, buffer, bufferOffset, charsToRead);
        return charsToRead;
    }

    /// <inheritdoc />
    public override DateTime GetDateTime(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to DateTime");

        return Convert.ToDateTime(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override decimal GetDecimal(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to decimal");

        return Convert.ToDecimal(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override double GetDouble(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to double");

        return Convert.ToDouble(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override float GetFloat(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to float");

        return Convert.ToSingle(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override Guid GetGuid(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to Guid");

        if (value is string str)
            return Guid.Parse(str);

        return (Guid)value;
    }

    /// <inheritdoc />
    public override short GetInt16(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to short");

        return Convert.ToInt16(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override int GetInt32(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to int");

        return Convert.ToInt32(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override long GetInt64(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to long");

        return Convert.ToInt64(value, CultureInfo.InvariantCulture);
    }

    /// <inheritdoc />
    public override string GetString(int ordinal)
    {
        var value = GetValue(ordinal);
        if (value == DBNull.Value)
            throw new InvalidCastException("Cannot convert null to string");

        return Convert.ToString(value, CultureInfo.InvariantCulture) ?? string.Empty;
    }

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
    {
        return new DbEnumerator(this);
    }

    private static object ConvertValue(HranaValue value)
    {
        if (value?.Value == null && string.IsNullOrEmpty(value?.Base64))
            return DBNull.Value;

        // Handle JsonElement which happens when deserializing from JSON
        if (value.Value is JsonElement element)
        {
            if (element.ValueKind == JsonValueKind.Null)
                return DBNull.Value;

            return value.Type switch
            {
                HranaTypes.Null => DBNull.Value,
                HranaTypes.Integer => element.ValueKind == JsonValueKind.String
                    ? long.Parse(element.GetString() ?? "0", CultureInfo.InvariantCulture)
                    : element.GetInt64(),
                HranaTypes.Float => element.ValueKind == JsonValueKind.String
                    ? double.Parse(element.GetString() ?? "0", CultureInfo.InvariantCulture)
                    : element.GetDouble(),
                HranaTypes.Text => element.GetString() ?? string.Empty,
                HranaTypes.Blob => !string.IsNullOrEmpty(value.Base64)
                    ? ConvertFromBase64(value.Base64)
                    : (element.ValueKind == JsonValueKind.String
                        ? ConvertFromBase64(element.GetString() ?? "")
                        : value.Value),
                _ => element.GetString() ?? string.Empty
            };
        }

        // Fallback for already-typed values
        return value.Type switch
        {
            HranaTypes.Null => DBNull.Value,
            HranaTypes.Integer => Convert.ToInt64(value.Value, CultureInfo.InvariantCulture),
            HranaTypes.Float => Convert.ToDouble(value.Value, CultureInfo.InvariantCulture),
            HranaTypes.Text => Convert.ToString(value.Value, CultureInfo.InvariantCulture) ?? string.Empty,
            HranaTypes.Blob => value.Base64 is not null
                ? ConvertFromBase64(value.Base64)
                : value.Value is string base64
                    ? ConvertFromBase64(base64)
                    : value.Value is byte[] bytes
                        ? bytes
                        : Array.Empty<byte>(),
            _ => value.Value
        };
    }

    private static byte[] ConvertFromBase64(string base64String)
    {
        if (string.IsNullOrEmpty(base64String))
        {
            return Array.Empty<byte>();
        }

        try
        {
            // Add missing Base64 padding if necessary (sqld strips padding from responses)
            var padded = base64String;
            var paddingNeeded = (4 - (base64String.Length % 4)) % 4;
            if (paddingNeeded > 0)
            {
                padded = base64String + new string('=', paddingNeeded);
            }

            return Convert.FromBase64String(padded);
        }
        catch (FormatException ex)
        {
            throw new LibSqlException($"Invalid Base64 string for blob data: '{base64String}'. Error: {ex.Message}", ex);
        }
    }
}
