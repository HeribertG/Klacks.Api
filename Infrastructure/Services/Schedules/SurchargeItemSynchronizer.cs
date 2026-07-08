// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Replaces the persisted typed surcharge items of a schedule entry with a freshly
/// computed set, using hard-delete-then-insert so recomputes never accumulate stale rows.
/// </summary>
using Klacks.Api.Domain.Models.Macros;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public static class SurchargeItemSynchronizer
{
    /// <summary>
    /// Removes the existing surcharge items matched by <paramref name="existingQuery"/> and adds the
    /// new items to <paramref name="navigation"/>. Both operations stay in the caller's change tracker
    /// so they persist atomically on the next SaveChanges.
    /// </summary>
    /// <param name="context">The scoped database context shared with the caller's unit of work</param>
    /// <param name="navigation">The parent's SurchargeItems navigation the new items are added to</param>
    /// <param name="existingQuery">Query selecting the parent's currently persisted surcharge items</param>
    /// <param name="items">The freshly computed typed surcharge portions from the macro result</param>
    /// <param name="sign">Multiplier applied to each amount (use -1 when the parent negates surcharges, e.g. unpaid breaks)</param>
    public static async Task ReplaceAsync(
        DataBaseContext context,
        ICollection<SurchargeItem> navigation,
        IQueryable<SurchargeItem> existingQuery,
        IReadOnlyList<MacroSurchargeItem> items,
        decimal sign = 1m)
    {
        var existing = await existingQuery.ToListAsync();
        if (existing.Count > 0)
        {
            context.SurchargeItem.RemoveRange(existing);
        }

        foreach (var item in items)
        {
            navigation.Add(new SurchargeItem { Type = item.Type, Amount = sign * item.Amount });
        }
    }
}
