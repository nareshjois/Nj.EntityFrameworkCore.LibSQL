using System.Text;

namespace Nj.LibSql.Data.Internal;

/// <summary>
/// Splits a SQL batch into individual statements (Microsoft.Data.Sqlite convenience batching parity).
/// libSQL's execute/prepare APIs only run the first statement; EF Core EnsureCreated HasData
/// sends multiple INSERTs in one CommandText.
/// </summary>
/// <remarks>
/// Semicolons inside <c>CREATE TRIGGER … BEGIN … END</c> bodies must not split the statement.
/// </remarks>
internal static class LibSqlStatementSplitter
{
    public static List<string> Split(string sql)
    {
        var statements = new List<string>();
        if (string.IsNullOrWhiteSpace(sql))
        {
            return statements;
        }

        var buffer = new StringBuilder(sql.Length);
        var inSingleQuote = false;
        var inDoubleQuote = false;
        var inLineComment = false;
        var inBlockComment = false;
        var awaitingTriggerBegin = false;
        var triggerBodyDepth = 0;

        for (var i = 0; i < sql.Length; i++)
        {
            var c = sql[i];
            var next = i + 1 < sql.Length ? sql[i + 1] : '\0';

            if (inLineComment)
            {
                buffer.Append(c);
                if (c is '\n' or '\r')
                {
                    inLineComment = false;
                }

                continue;
            }

            if (inBlockComment)
            {
                buffer.Append(c);
                if (c == '*' && next == '/')
                {
                    buffer.Append(next);
                    i++;
                    inBlockComment = false;
                }

                continue;
            }

            if (!inDoubleQuote && c == '\'')
            {
                buffer.Append(c);
                if (inSingleQuote && next == '\'')
                {
                    buffer.Append(next);
                    i++;
                }
                else
                {
                    inSingleQuote = !inSingleQuote;
                }

                continue;
            }

            if (!inSingleQuote && c == '"')
            {
                inDoubleQuote = !inDoubleQuote;
                buffer.Append(c);
                continue;
            }

            if (!inSingleQuote && !inDoubleQuote)
            {
                if (c == '-' && next == '-')
                {
                    inLineComment = true;
                    buffer.Append(c);
                    continue;
                }

                if (c == '/' && next == '*')
                {
                    inBlockComment = true;
                    buffer.Append(c);
                    continue;
                }

                if (TryMatchKeyword(sql, i, "CREATE", out var createEnd)
                    && LooksLikeCreateTrigger(sql, createEnd))
                {
                    awaitingTriggerBegin = true;
                }
                else if (awaitingTriggerBegin && TryMatchKeyword(sql, i, "BEGIN", out _))
                {
                    awaitingTriggerBegin = false;
                    triggerBodyDepth = 1;
                }
                else if (triggerBodyDepth > 0 && TryMatchKeyword(sql, i, "BEGIN", out _))
                {
                    triggerBodyDepth++;
                }
                else if (triggerBodyDepth > 0 && TryMatchKeyword(sql, i, "END", out _))
                {
                    triggerBodyDepth--;
                }

                if (c == ';' && triggerBodyDepth == 0 && !awaitingTriggerBegin)
                {
                    AddStatement(statements, buffer);
                    buffer.Clear();
                    awaitingTriggerBegin = false;
                    continue;
                }
            }

            buffer.Append(c);
        }

        AddStatement(statements, buffer);
        return statements;
    }

    /// <summary>
    /// After <c>CREATE</c>, skip optional TEMP/TEMPORARY/IF NOT EXISTS and detect <c>TRIGGER</c>.
    /// </summary>
    private static bool LooksLikeCreateTrigger(string sql, int afterCreate)
    {
        var i = afterCreate;
        while (true)
        {
            i = SkipWhitespaceAndComments(sql, i);
            if (i >= sql.Length)
            {
                return false;
            }

            if (TryMatchKeyword(sql, i, "TEMP", out var end)
                || TryMatchKeyword(sql, i, "TEMPORARY", out end))
            {
                i = end;
                continue;
            }

            if (TryMatchKeyword(sql, i, "IF", out end))
            {
                i = SkipWhitespaceAndComments(sql, end);
                if (!TryMatchKeyword(sql, i, "NOT", out end))
                {
                    return false;
                }

                i = SkipWhitespaceAndComments(sql, end);
                if (!TryMatchKeyword(sql, i, "EXISTS", out end))
                {
                    return false;
                }

                i = end;
                continue;
            }

            return TryMatchKeyword(sql, i, "TRIGGER", out _);
        }
    }

    private static int SkipWhitespaceAndComments(string sql, int i)
    {
        while (i < sql.Length)
        {
            var c = sql[i];
            if (char.IsWhiteSpace(c))
            {
                i++;
                continue;
            }

            if (c == '-' && i + 1 < sql.Length && sql[i + 1] == '-')
            {
                i += 2;
                while (i < sql.Length && sql[i] is not ('\n' or '\r'))
                {
                    i++;
                }

                continue;
            }

            if (c == '/' && i + 1 < sql.Length && sql[i + 1] == '*')
            {
                i += 2;
                while (i + 1 < sql.Length && !(sql[i] == '*' && sql[i + 1] == '/'))
                {
                    i++;
                }

                i = Math.Min(i + 2, sql.Length);
                continue;
            }

            break;
        }

        return i;
    }

    private static bool TryMatchKeyword(string sql, int i, string keyword, out int end)
    {
        end = i;
        if (i > 0 && IsIdentifierChar(sql[i - 1]))
        {
            return false;
        }

        if (i + keyword.Length > sql.Length)
        {
            return false;
        }

        if (!sql.AsSpan(i).StartsWith(keyword, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        end = i + keyword.Length;
        if (end < sql.Length && IsIdentifierChar(sql[end]))
        {
            return false;
        }

        return true;
    }

    private static bool IsIdentifierChar(char c)
        => char.IsLetterOrDigit(c) || c is '_' or '$';

    private static void AddStatement(List<string> statements, StringBuilder buffer)
    {
        var statement = buffer.ToString().Trim();
        if (statement.Length > 0)
        {
            statements.Add(statement);
        }
    }
}
