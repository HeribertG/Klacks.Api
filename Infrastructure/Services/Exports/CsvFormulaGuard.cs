// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Neutralises CSV formula injection (CWE-1236) by prefixing values that begin with a
/// spreadsheet formula trigger character with a single quote, so Excel/LibreOffice treat
/// the cell as literal text instead of evaluating it.
/// </summary>
namespace Klacks.Api.Infrastructure.Services.Exports;

internal static class CsvFormulaGuard
{
    private const string FormulaTriggerChars = "=+-@\t\r";

    /// <summary>
    /// Returns the value unchanged unless it starts with a formula trigger character,
    /// in which case a single quote is prepended to force text interpretation.
    /// </summary>
    /// <param name="value">Untrusted cell value that may contain user-controlled text</param>
    public static string Neutralize(string value)
    {
        if (!string.IsNullOrEmpty(value) && FormulaTriggerChars.IndexOf(value[0]) >= 0)
        {
            return "'" + value;
        }

        return value;
    }
}
