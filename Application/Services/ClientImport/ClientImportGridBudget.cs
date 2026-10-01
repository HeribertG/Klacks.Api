// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Total character budget of an import grid (all cells of all data rows). Parse checks the grid it sends
/// to the browser and the request guard checks the same grid when it comes back, with this one measure,
/// so a grid Parse accepted can never be rejected by Preview or Commit for its size.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.ClientImport;

public static class ClientImportGridBudget
{
    public static long CountChars(IEnumerable<IReadOnlyList<string?>?> rows)
    {
        long total = 0;
        foreach (var row in rows)
        {
            if (row == null)
            {
                continue;
            }

            foreach (var cell in row)
            {
                total += cell?.Length ?? 0;
            }
        }

        return total;
    }

    public static bool Exceeds(IEnumerable<IReadOnlyList<string?>?> rows) => CountChars(rows) > ClientImportLimits.MaxGridChars;
}
