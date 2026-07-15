// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Text.RegularExpressions;

namespace Nj.EntityFrameworkCore.LibSql.Scaffolding.Internal;

/// <summary>
///     Parses column COLLATE / AUTOINCREMENT facets from <c>sqlite_master.sql</c>.
///     Used because Nelknet does not expose <c>sqlite3_table_column_metadata</c>
///     (and HTTP connections cannot use that native API anyway).
/// </summary>
internal static partial class LibSqlCreateSqlColumnFacets
{
    [GeneratedRegex(@"\bCOLLATE\s+(""(?:[^""]|"""")*""|'(?:[^']|'')*'|\[(?:[^\]])*\]|`(?:[^`])*`|[A-Za-z_][\w]*)",
        RegexOptions.IgnoreCase | RegexOptions.CultureInvariant)]
    private static partial Regex CollateRegex();

    public static void GetFacets(string? createSql, string columnName, out string? collation, out bool autoIncrement)
    {
        collation = null;
        autoIncrement = false;

        if (string.IsNullOrWhiteSpace(createSql)
            || createSql.StartsWith("CREATE VIEW", StringComparison.OrdinalIgnoreCase))
        {
            return;
        }

        var definition = TryGetColumnDefinition(createSql, columnName);
        if (definition is null)
        {
            return;
        }

        autoIncrement = definition.Contains("AUTOINCREMENT", StringComparison.OrdinalIgnoreCase);

        var match = CollateRegex().Match(definition);
        if (match.Success)
        {
            collation = UnquoteIdentifier(match.Groups[1].Value);
        }
    }

    private static string? TryGetColumnDefinition(string createSql, string columnName)
    {
        var open = createSql.IndexOf('(');
        var close = createSql.LastIndexOf(')');
        if (open < 0 || close <= open)
        {
            return null;
        }

        var body = createSql.Substring(open + 1, close - open - 1);
        foreach (var segment in SplitTopLevelCommaSeparated(body))
        {
            var trimmed = segment.Trim();
            if (trimmed.Length == 0 || IsTableConstraint(trimmed))
            {
                continue;
            }

            if (!TryReadIdentifier(trimmed, out var name, out var restStart))
            {
                continue;
            }

            if (!string.Equals(name, columnName, StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            return trimmed[restStart..].TrimStart();
        }

        return null;
    }

    private static bool IsTableConstraint(string segment)
    {
        return StartsWithKeyword(segment, "CONSTRAINT")
            || StartsWithKeyword(segment, "PRIMARY")
            || StartsWithKeyword(segment, "UNIQUE")
            || StartsWithKeyword(segment, "CHECK")
            || StartsWithKeyword(segment, "FOREIGN");
    }

    private static bool StartsWithKeyword(string text, string keyword)
        => text.Length >= keyword.Length
            && text.StartsWith(keyword, StringComparison.OrdinalIgnoreCase)
            && (text.Length == keyword.Length || !IsIdentifierChar(text[keyword.Length]));

    private static bool IsIdentifierChar(char c)
        => char.IsLetterOrDigit(c) || c is '_' or '$';

    private static bool TryReadIdentifier(string text, out string name, out int restStart)
    {
        name = "";
        restStart = 0;
        if (text.Length == 0)
        {
            return false;
        }

        var i = 0;
        while (i < text.Length && char.IsWhiteSpace(text[i]))
        {
            i++;
        }

        if (i >= text.Length)
        {
            return false;
        }

        if (text[i] is '"' or '\'' or '`' or '[')
        {
            var quote = text[i];
            var endQuote = quote == '[' ? ']' : quote;
            i++;
            var start = i;
            while (i < text.Length && text[i] != endQuote)
            {
                // SQLite doubles quotes inside quoted identifiers.
                if (quote is '"' or '\'' && i + 1 < text.Length && text[i] == quote && text[i + 1] == quote)
                {
                    i += 2;
                    continue;
                }

                i++;
            }

            if (i >= text.Length)
            {
                return false;
            }

            name = text[start..i].Replace($"{quote}{quote}", $"{quote}", StringComparison.Ordinal);
            restStart = i + 1;
            return true;
        }

        var idStart = i;
        while (i < text.Length && IsIdentifierChar(text[i]))
        {
            i++;
        }

        if (i == idStart)
        {
            return false;
        }

        name = text[idStart..i];
        restStart = i;
        return true;
    }

    private static string UnquoteIdentifier(string value)
    {
        if (value.Length >= 2)
        {
            if (value[0] == '"' && value[^1] == '"')
            {
                return value[1..^1].Replace("\"\"", "\"", StringComparison.Ordinal);
            }

            if (value[0] == '\'' && value[^1] == '\'')
            {
                return value[1..^1].Replace("''", "'", StringComparison.Ordinal);
            }

            if (value[0] == '`' && value[^1] == '`')
            {
                return value[1..^1];
            }

            if (value[0] == '[' && value[^1] == ']')
            {
                return value[1..^1];
            }
        }

        return value;
    }

    private static List<string> SplitTopLevelCommaSeparated(string body)
    {
        var parts = new List<string>();
        var depth = 0;
        var inSingle = false;
        var inDouble = false;
        var inBacktick = false;
        var inBracket = false;
        var start = 0;

        for (var i = 0; i < body.Length; i++)
        {
            var c = body[i];

            if (inSingle)
            {
                if (c == '\'')
                {
                    if (i + 1 < body.Length && body[i + 1] == '\'')
                    {
                        i++;
                    }
                    else
                    {
                        inSingle = false;
                    }
                }

                continue;
            }

            if (inDouble)
            {
                if (c == '"')
                {
                    if (i + 1 < body.Length && body[i + 1] == '"')
                    {
                        i++;
                    }
                    else
                    {
                        inDouble = false;
                    }
                }

                continue;
            }

            if (inBacktick)
            {
                if (c == '`')
                {
                    inBacktick = false;
                }

                continue;
            }

            if (inBracket)
            {
                if (c == ']')
                {
                    inBracket = false;
                }

                continue;
            }

            switch (c)
            {
                case '\'':
                    inSingle = true;
                    break;
                case '"':
                    inDouble = true;
                    break;
                case '`':
                    inBacktick = true;
                    break;
                case '[':
                    inBracket = true;
                    break;
                case '(':
                    depth++;
                    break;
                case ')':
                    if (depth > 0)
                    {
                        depth--;
                    }

                    break;
                case ',' when depth == 0:
                    parts.Add(body[start..i]);
                    start = i + 1;
                    break;
            }
        }

        if (start < body.Length)
        {
            parts.Add(body[start..]);
        }

        return parts;
    }
}
