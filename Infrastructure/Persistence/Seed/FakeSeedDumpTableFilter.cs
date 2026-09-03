// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes whole INSERT statements targeting specific tables from a raw SQL seed dump, keeping every
/// other statement and any non-statement content (comments, SET statements, blank lines) untouched.
/// </summary>

using System.Text;
using System.Text.RegularExpressions;

namespace Klacks.Api.Data.Seed;

public static class FakeSeedDumpTableFilter
{
    private const char StatementTerminator = ';';

    private const string CommentPrefix = "--";

    private static readonly Regex InsertTargetTablePattern = new(
        @"^\s*INSERT\s+INTO\s+(?:public\.)?""?([A-Za-z_][A-Za-z0-9_]*)""?",
        RegexOptions.IgnoreCase | RegexOptions.Compiled);

    public static string RemoveTables(string dumpSql, IReadOnlySet<string> excludedTableNames)
    {
        var result = new StringBuilder();
        var statement = new StringBuilder();
        var lines = dumpSql.Split('\n');

        for (var i = 0; i < lines.Length; i++)
        {
            var line = lines[i];
            var isTrailingSplitArtifact = i == lines.Length - 1 && line.Length == 0;

            if (isTrailingSplitArtifact)
            {
                break;
            }

            var trimmedStart = line.TrimStart();
            var isStandaloneNonStatementLine = statement.Length == 0 &&
                (trimmedStart.Length == 0 || trimmedStart.StartsWith(CommentPrefix, StringComparison.Ordinal));

            if (isStandaloneNonStatementLine)
            {
                result.Append(line).Append('\n');
                continue;
            }

            statement.Append(line).Append('\n');

            if (!line.TrimEnd().EndsWith(StatementTerminator))
            {
                continue;
            }

            AppendStatementUnlessExcluded(result, statement.ToString(), excludedTableNames);
            statement.Clear();
        }

        if (statement.Length > 0)
        {
            AppendStatementUnlessExcluded(result, statement.ToString(), excludedTableNames);
        }

        return result.ToString();
    }

    private static void AppendStatementUnlessExcluded(StringBuilder result, string statement, IReadOnlySet<string> excludedTableNames)
    {
        var match = InsertTargetTablePattern.Match(statement);

        if (match.Success && excludedTableNames.Contains(match.Groups[1].Value))
        {
            return;
        }

        result.Append(statement);
    }
}
