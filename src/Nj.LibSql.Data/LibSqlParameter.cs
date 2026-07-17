using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;
using Nj.LibSql.Data.Internal;

namespace Nj.LibSql.Data;

/// <summary>Represents a parameter to a <see cref="LibSqlCommand"/>.</summary>
public sealed class LibSqlParameter : DbParameter
{
    private string _parameterName = string.Empty;
    private object? _value;
    private DbType _dbType = DbType.String;
    private ParameterDirection _direction = ParameterDirection.Input;

    /// <summary>Initializes a new instance of the <see cref="LibSqlParameter"/> class.</summary>
    public LibSqlParameter()
    {
    }

    /// <summary>Initializes a new instance of the <see cref="LibSqlParameter"/> class.</summary>
    public LibSqlParameter(string parameterName)
        => ParameterName = parameterName;

    /// <summary>Initializes a new instance of the <see cref="LibSqlParameter"/> class.</summary>
    public LibSqlParameter(string parameterName, object? value)
        : this(parameterName)
        => Value = value;

    /// <summary>Initializes a new instance of the <see cref="LibSqlParameter"/> class.</summary>
    public LibSqlParameter(string parameterName, DbType dbType)
        : this(parameterName)
        => DbType = dbType;

    /// <inheritdoc />
    public override DbType DbType
    {
        get => _dbType;
        set => _dbType = value;
    }

    /// <inheritdoc />
    public override ParameterDirection Direction
    {
        get => _direction;
        set
        {
            if (value != ParameterDirection.Input)
            {
                throw new NotSupportedException("libSQL only supports input parameters.");
            }

            _direction = value;
        }
    }

    /// <inheritdoc />
    public override bool IsNullable { get; set; } = true;

    /// <inheritdoc />
    [AllowNull]
    public override string ParameterName
    {
        get => _parameterName;
        set => _parameterName = NormalizeParameterName(value);
    }

    /// <inheritdoc />
    public override int Size { get; set; }

    /// <inheritdoc />
    [AllowNull]
    public override string SourceColumn { get; set; } = string.Empty;

    /// <inheritdoc />
    public override bool SourceColumnNullMapping { get; set; }

    /// <inheritdoc />
    public override DataRowVersion SourceVersion { get; set; } = DataRowVersion.Current;

    /// <inheritdoc />
    public override object? Value
    {
        get => _value;
        set
        {
            _value = value;
            if (value != null && value != DBNull.Value)
            {
                _dbType = LibSqlTypeMapper.GetDbType(value.GetType());

                Size = value switch
                {
                    string s => s.Length,
                    byte[] b => b.Length,
                    _ => Size
                };
            }
        }
    }

    /// <inheritdoc />
    public override void ResetDbType()
        => _dbType = DbType.String;

    /// <inheritdoc />
    public override byte Precision { get; set; }

    /// <inheritdoc />
    public override byte Scale { get; set; }

    /// <summary>Validates the parameter's properties.</summary>
    public void Validate()
    {
        if (string.IsNullOrEmpty(ParameterName))
        {
            throw new InvalidOperationException("Parameter name cannot be null or empty.");
        }

        ParameterName = NormalizeParameterName(ParameterName);
    }

    /// <summary>Gets the libSQL database type for this parameter.</summary>
    internal LibSqlDbType LibSqlType => LibSqlTypeMapper.GetLibSqlType(_dbType);

    /// <summary>Gets the value converted for libSQL binding.</summary>
    internal object GetLibSqlValue()
        => LibSqlTypeConverter.ConvertToLibSql(_value, LibSqlType);

    private static string NormalizeParameterName(string? value)
    {
        var name = value ?? string.Empty;
        if (string.IsNullOrEmpty(name))
        {
            return name;
        }

        // EF Core FromSqlInterpolated passes names like "p0" while the SQL text uses "@p0".
        // Microsoft.Data.Sqlite accepts either form; normalize so ADO.NET/EF callers work.
        if (!name.StartsWith('@') && !name.StartsWith(':') && !name.StartsWith('$') && !name.StartsWith('?'))
        {
            return "@" + name;
        }

        return name;
    }
}
