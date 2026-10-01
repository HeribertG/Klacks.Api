// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Finds the header row of an import grid: the first of the leading rows with at least two non-empty
/// cells that are text rather than numbers or dates. A single title line such as "Staff list 2026"
/// above the real header is skipped that way.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportHeaderRowLocator
{
    public static int? Locate(IReadOnlyList<IReadOnlyList<string>> rows)
    {
        var limit = Math.Min(rows.Count, ClientImportLimits.HeaderSearchRows);
        for (var index = 0; index < limit; index++)
        {
            var textCells = rows[index].Count(IsTextCell);
            if (textCells >= ClientImportLimits.MinHeaderCells)
            {
                return index;
            }
        }

        return null;
    }

    private static bool IsTextCell(string value) =>
        !string.IsNullOrWhiteSpace(value)
        && value.Any(char.IsLetter)
        && !ClientImportValueClassifier.IsDate(value)
        && !ClientImportValueClassifier.IsEmail(value);
}
