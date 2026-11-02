// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The repository's last line for shifts that carry a token: a scenario clone or any other token cannot be edited or
/// deleted in real mode. The exception is the token of a live variant (key), whose shifts are written through the
/// handlers; whether that variant is still Active and visible is checked before, by IVariantShiftWriteGuard. A static
/// helper, because the repository already holds the context and has more than twenty test construction sites.
/// </summary>
/// <param name="context">Database context of the calling repository</param>
/// <param name="stored">The shift as stored</param>

using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

internal static class ShiftScenarioWriteGate
{
    public static async Task EnsureEditableAsync(DataBaseContext context, Shift stored)
    {
        if (await IsRefusedAsync(context, stored))
        {
            throw new InvalidOperationException(
                $"Shift {stored.Id} cannot be edited while in scenario mode (AnalyseToken={stored.AnalyseToken}).");
        }
    }

    public static async Task EnsureDeletableAsync(DataBaseContext context, Shift stored)
    {
        if (await IsRefusedAsync(context, stored))
        {
            throw new InvalidOperationException(
                $"Shift {stored.Id} cannot be deleted while in scenario mode (AnalyseToken={stored.AnalyseToken}).");
        }
    }

    private static async Task<bool> IsRefusedAsync(DataBaseContext context, Shift stored)
    {
        return stored.AnalyseToken is { } token
            && !await ScenarioVariantQueries.LiveVariants(context).AnyAsync(v => v.Token == token);
    }
}
