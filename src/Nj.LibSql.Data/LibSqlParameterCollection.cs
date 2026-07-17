using System.Collections;
using System.Data;
using System.Data.Common;

namespace Nj.LibSql.Data;

/// <summary>Represents a collection of parameters associated with a <see cref="LibSqlCommand"/>.</summary>
public sealed class LibSqlParameterCollection : DbParameterCollection
{
    private readonly List<LibSqlParameter> _parameters = [];

    /// <inheritdoc />
    public override int Count => _parameters.Count;

    /// <inheritdoc />
    public override bool IsFixedSize => false;

    /// <inheritdoc />
    public override bool IsReadOnly => false;

    /// <inheritdoc />
    public override bool IsSynchronized => false;

    /// <inheritdoc />
    public override object SyncRoot => ((ICollection)_parameters).SyncRoot;

    /// <inheritdoc />
    public new LibSqlParameter this[int index]
    {
        get => _parameters[index];
        set
        {
            ArgumentNullException.ThrowIfNull(value);
            _parameters[index] = value;
        }
    }

    /// <inheritdoc />
    public new LibSqlParameter this[string parameterName]
    {
        get => (LibSqlParameter)GetParameter(parameterName);
        set => SetParameter(parameterName, value);
    }

    /// <inheritdoc />
    public override int Add(object value)
    {
        if (value is not LibSqlParameter parameter)
        {
            throw new ArgumentException("Value must be a LibSqlParameter.", nameof(value));
        }

        _parameters.Add(parameter);
        return _parameters.Count - 1;
    }

    /// <summary>Add(LibSqlParameter.</summary>
    public LibSqlParameter Add(LibSqlParameter parameter)
    {
        ArgumentNullException.ThrowIfNull(parameter);

        _parameters.Add(parameter);
        return parameter;
    }

    /// <summary>AddWithValue(string.</summary>
    public LibSqlParameter AddWithValue(string parameterName, object? value)
    {
        var parameter = new LibSqlParameter(parameterName, value);
        Add(parameter);
        return parameter;
    }

    /// <summary>AddWithValue(string.</summary>
    public LibSqlParameter AddWithValue(string parameterName, DbType dbType, object? value)
    {
        var parameter = new LibSqlParameter(parameterName, dbType) { Value = value };
        parameter.DbType = dbType;
        Add(parameter);
        return parameter;
    }

    /// <summary>AddRange(IEnumerable.</summary>
    public void AddRange(IEnumerable<LibSqlParameter> parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        foreach (var parameter in parameters)
        {
            Add(parameter);
        }
    }

    /// <inheritdoc />
    public override void AddRange(Array values)
    {
        ArgumentNullException.ThrowIfNull(values);

        foreach (LibSqlParameter parameter in values)
        {
            Add(parameter);
        }
    }

    /// <inheritdoc />
    public override void Clear()
        => _parameters.Clear();

    /// <inheritdoc />
    public override bool Contains(object value)
        => value is LibSqlParameter parameter && _parameters.Contains(parameter);

    /// <inheritdoc />
    public override bool Contains(string value)
        => IndexOf(value) >= 0;

    /// <inheritdoc />
    public override void CopyTo(Array array, int index)
        => ((ICollection)_parameters).CopyTo(array, index);

    /// <inheritdoc />
    public override IEnumerator GetEnumerator()
        => _parameters.GetEnumerator();

    /// <inheritdoc />
    public override int IndexOf(object value)
        => value is LibSqlParameter parameter ? _parameters.IndexOf(parameter) : -1;

    /// <inheritdoc />
    public override int IndexOf(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            return -1;
        }

        for (var i = 0; i < _parameters.Count; i++)
        {
            if (string.Equals(_parameters[i].ParameterName, parameterName, StringComparison.OrdinalIgnoreCase))
            {
                return i;
            }
        }

        return -1;
    }

    /// <summary>Validates all parameters in the collection.</summary>
    public void ValidateParameters()
    {
        var parameterNames = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        foreach (var parameter in _parameters)
        {
            parameter.Validate();

            if (!parameterNames.Add(parameter.ParameterName))
            {
                throw new InvalidOperationException($"Duplicate parameter name: {parameter.ParameterName}");
            }
        }
    }

    /// <inheritdoc />
    public override void Insert(int index, object value)
    {
        if (value is not LibSqlParameter parameter)
        {
            throw new ArgumentException("Value must be a LibSqlParameter.", nameof(value));
        }

        _parameters.Insert(index, parameter);
    }

    /// <inheritdoc />
    public override void Remove(object value)
    {
        if (value is LibSqlParameter parameter)
        {
            _parameters.Remove(parameter);
        }
    }

    /// <inheritdoc />
    public override void RemoveAt(int index)
        => _parameters.RemoveAt(index);

    /// <inheritdoc />
    public override void RemoveAt(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index >= 0)
        {
            RemoveAt(index);
        }
    }

    /// <inheritdoc />
    protected override DbParameter GetParameter(int index)
        => _parameters[index];

    /// <inheritdoc />
    protected override DbParameter GetParameter(string parameterName)
    {
        var index = IndexOf(parameterName);
        if (index < 0)
        {
            throw new ArgumentException($"Parameter '{parameterName}' not found.", nameof(parameterName));
        }

        return _parameters[index];
    }

    /// <inheritdoc />
    protected override void SetParameter(int index, DbParameter value)
        => _parameters[index] = (LibSqlParameter)value;

    /// <inheritdoc />
    protected override void SetParameter(string parameterName, DbParameter value)
    {
        var index = IndexOf(parameterName);
        if (index < 0)
        {
            throw new ArgumentException($"Parameter '{parameterName}' not found.", nameof(parameterName));
        }

        _parameters[index] = (LibSqlParameter)value;
    }
}
