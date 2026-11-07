// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Which breaks count their hours and surcharges toward the period hours and the target of a person: every break except
/// an absence of an on-call type (Absence.IsOnCall) that stands on its own. An on-call absence stands for on-call duty
/// outside the company, of which only the actual call-out counts, and that call-out is planned as its own work (owner
/// decision 2026-11-07; Swiss basis ArGV 1 Art. 15). A container sub-break (ParentWorkId set) is part of the container's
/// time and keeps counting whatever its type, so an unpaid span inside a container stays deducted. The rule filters on
/// AbsenceId against the given on-call type ids instead of the Absence navigation, so a retired (soft-deleted) on-call
/// type keeps its meaning when the ids come from OnCallAbsenceQuery.
/// </summary>
/// <param name="onCallAbsenceIds">Ids of the on-call absence types, soft-deleted types included</param>

using System.Linq.Expressions;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class BreakOwnHours
{
    public static Expression<Func<Break, bool>> Counts(IReadOnlyCollection<Guid> onCallAbsenceIds)
    {
        var ids = onCallAbsenceIds.ToList();
        return b => b.ParentWorkId != null || !ids.Contains(b.AbsenceId);
    }

    /// <summary>
    /// The hours a break contributes in memory, from the same rule as <see cref="Counts"/>: its WorkTime when it counts,
    /// otherwise zero.
    /// </summary>
    /// <param name="onCallAbsenceIds">Ids of the on-call absence types, soft-deleted types included</param>
    public static Func<Break, decimal> CountedHours(IReadOnlyCollection<Guid> onCallAbsenceIds)
    {
        var counts = Counts(onCallAbsenceIds).Compile();
        return b => counts(b) ? b.WorkTime : 0m;
    }
}
