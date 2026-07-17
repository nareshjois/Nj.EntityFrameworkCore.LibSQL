using System.Text;

namespace Nj.LibSql.Data.Internal;

/// <summary>
/// Parses SQL text for <c>?</c>, <c>?N</c>, <c>@name</c>, <c>:name</c> and <c>$name</c> parameter
/// markers and resolves an ADO.NET <see cref="LibSqlParameterCollection"/> against the resulting
/// SQLite bind positions.
/// </summary>
internal sealed class SqlParameterLayout
{
    private readonly List<SqlParameterToken> _tokens;
    private readonly Dictionary<string, int> _namedPositions;
    private readonly HashSet<int> _requiredPositions;
    private readonly List<int> _requiredPositionsInSqlOrder;
    private readonly List<int> _positionalPositionsInSqlOrder;
    private readonly bool _hasNamedParameters;

    private SqlParameterLayout(List<SqlParameterToken> tokens, Dictionary<string, int> namedPositions, int maxPosition)
    {
        _tokens = tokens;
        _namedPositions = namedPositions;
        MaxPosition = maxPosition;
        _hasNamedParameters = tokens.Any(token => token.Name != null);
        _requiredPositions = tokens.Select(token => token.Position).ToHashSet();
        _requiredPositionsInSqlOrder = tokens
            .Select(token => token.Position)
            .Distinct()
            .ToList();
        _positionalPositionsInSqlOrder = tokens
            .Where(token => token.Name == null)
            .Select(token => token.Position)
            .Distinct()
            .ToList();
    }

    internal int MaxPosition { get; }

    internal static SqlParameterLayout Parse(string sql)
    {
        var tokens = new List<SqlParameterToken>();
        var namedPositions = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);

        if (string.IsNullOrEmpty(sql))
        {
            return new SqlParameterLayout(tokens, namedPositions, 0);
        }

        var maxPosition = 0;
        var i = 0;
        var length = sql.Length;

        while (i < length)
        {
            var c = sql[i];

            if (c == '-' && i + 1 < length && sql[i + 1] == '-')
            {
                var newline = sql.IndexOf('\n', i + 2);
                i = newline < 0 ? length : newline + 1;
                continue;
            }

            if (c == '/' && i + 1 < length && sql[i + 1] == '*')
            {
                var close = sql.IndexOf("*/", i + 2, StringComparison.Ordinal);
                i = close < 0 ? length : close + 2;
                continue;
            }

            if (c == '\'')
            {
                i = SkipQuoted(sql, i, '\'');
                continue;
            }

            if (c == '"')
            {
                i = SkipQuoted(sql, i, '"');
                continue;
            }

            if (c == '[')
            {
                var close = sql.IndexOf(']', i + 1);
                i = close < 0 ? length : close + 1;
                continue;
            }

            if (c == '`')
            {
                i = SkipQuoted(sql, i, '`');
                continue;
            }

            if (c == '?')
            {
                var start = i;
                i++;
                var digitsStart = i;
                while (i < length && char.IsDigit(sql[i]))
                {
                    i++;
                }

                int position;
                if (i > digitsStart
                    && int.TryParse(sql.AsSpan(digitsStart, i - digitsStart), out var explicitPosition)
                    && explicitPosition > 0)
                {
                    position = explicitPosition;
                    maxPosition = Math.Max(maxPosition, position);
                }
                else
                {
                    position = maxPosition + 1;
                    maxPosition = position;
                }

                tokens.Add(new SqlParameterToken(start, i - start, position, null));
                continue;
            }

            if (c == '@' || c == ':' || c == '$')
            {
                var start = i;
                i++;
                while (i < length && (char.IsLetterOrDigit(sql[i]) || sql[i] == '_'))
                {
                    i++;
                }

                if (i > start + 1)
                {
                    var name = sql.Substring(start, i - start);
                    if (!namedPositions.TryGetValue(name, out var position))
                    {
                        position = maxPosition + 1;
                        namedPositions.Add(name, position);
                        maxPosition = position;
                    }

                    tokens.Add(new SqlParameterToken(start, i - start, position, name));
                }

                continue;
            }

            i++;
        }

        return new SqlParameterLayout(tokens, namedPositions, maxPosition);
    }

    internal string ToIndexedParameterSql(string sql)
    {
        if (_tokens.Count == 0)
        {
            return sql;
        }

        var builder = new StringBuilder(sql.Length + (_tokens.Count * 2));
        var offset = 0;

        foreach (var token in _tokens)
        {
            builder.Append(sql, offset, token.Start - offset);
            builder.Append('?');
            builder.Append(token.Position);
            offset = token.Start + token.Length;
        }

        builder.Append(sql, offset, sql.Length - offset);
        return builder.ToString();
    }

    internal IReadOnlyList<SqlParameterBinding> ResolveBindings(LibSqlParameterCollection parameters)
    {
        ArgumentNullException.ThrowIfNull(parameters);

        if (_tokens.Count == 0 || parameters.Count == 0)
        {
            return [];
        }

        var bindings = new List<SqlParameterBinding>();
        var boundPositions = new HashSet<int>();
        var nextPositionalParameter = 0;

        for (var i = 0; i < parameters.Count; i++)
        {
            var parameter = parameters[i];
            var position = ResolvePosition(parameter.ParameterName, i, boundPositions, ref nextPositionalParameter);
            if (position == null)
            {
                continue;
            }

            if (!boundPositions.Add(position.Value))
            {
                throw new InvalidOperationException($"Multiple parameters resolve to SQL bind position {position.Value}.");
            }

            bindings.Add(new SqlParameterBinding(parameter, position.Value));
        }

        foreach (var requiredPosition in _requiredPositions)
        {
            if (!boundPositions.Contains(requiredPosition))
            {
                throw new InvalidOperationException($"Missing value for SQL parameter {DescribePosition(requiredPosition)}.");
            }
        }

        return bindings;
    }

    private int? ResolvePosition(string parameterName, int collectionIndex, HashSet<int> boundPositions, ref int nextPositionalParameter)
    {
        if (_namedPositions.TryGetValue(parameterName, out var namedPosition))
        {
            return namedPosition;
        }

        // Accept @name / :name / $name interchangeably when the SQL used a different prefix.
        foreach (var alias in ExpandNamedAliases(parameterName))
        {
            if (_namedPositions.TryGetValue(alias, out namedPosition))
            {
                return namedPosition;
            }
        }

        if (TryParseExplicitPosition(parameterName, out var explicitPosition) && _requiredPositions.Contains(explicitPosition))
        {
            return explicitPosition;
        }

        if (parameterName == "?")
        {
            return ResolveNextPositionalPosition(boundPositions, ref nextPositionalParameter);
        }

        if (!_hasNamedParameters && collectionIndex < _requiredPositionsInSqlOrder.Count)
        {
            return _requiredPositionsInSqlOrder[collectionIndex];
        }

        return null;
    }

    private static IEnumerable<string> ExpandNamedAliases(string parameterName)
    {
        if (string.IsNullOrEmpty(parameterName))
        {
            yield break;
        }

        var core = parameterName[0] is '@' or ':' or '$'
            ? parameterName[1..]
            : parameterName;
        if (string.IsNullOrEmpty(core))
        {
            yield break;
        }

        yield return "@" + core;
        yield return ":" + core;
        yield return "$" + core;
        yield return core;
    }

    private int? ResolveNextPositionalPosition(HashSet<int> boundPositions, ref int nextPositionalParameter)
    {
        while (nextPositionalParameter < _positionalPositionsInSqlOrder.Count)
        {
            var position = _positionalPositionsInSqlOrder[nextPositionalParameter++];
            if (!boundPositions.Contains(position))
            {
                return position;
            }
        }

        return null;
    }

    private string DescribePosition(int position)
    {
        var named = _tokens.FirstOrDefault(token => token.Position == position && token.Name != null);
        return named.Name ?? $"at position {position}";
    }

    private static bool TryParseExplicitPosition(string parameterName, out int position)
    {
        position = 0;
        if (parameterName.Length <= 1 || parameterName[0] != '?')
        {
            return false;
        }

        return int.TryParse(parameterName.AsSpan(1), out position) && position > 0;
    }

    private static int SkipQuoted(string sql, int start, char quote)
    {
        var i = start + 1;
        while (i < sql.Length)
        {
            if (sql[i] == quote)
            {
                if (i + 1 < sql.Length && sql[i + 1] == quote)
                {
                    i += 2;
                    continue;
                }

                return i + 1;
            }

            i++;
        }

        return sql.Length;
    }
}

internal readonly struct SqlParameterBinding(LibSqlParameter parameter, int position)
{
    internal LibSqlParameter Parameter { get; } = parameter;

    internal int Position { get; } = position;
}

internal readonly struct SqlParameterToken(int start, int length, int position, string? name)
{
    internal int Start { get; } = start;

    internal int Length { get; } = length;

    internal int Position { get; } = position;

    internal string? Name { get; } = name;
}
