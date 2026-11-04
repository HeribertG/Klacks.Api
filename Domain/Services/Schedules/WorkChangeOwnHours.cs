// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Which WorkChanges count their hours and surcharges in the sums of a person or a customer: every change outside a
/// container, and a replacement on an inner service. A correction-family change on an inner service (its travel time,
/// briefing or debriefing) lies inside the container's time, which the container already counts, so it counts for no
/// one; it stays for the fit check, the display and the customer invoice. Unlike WorkOwnHours, which keeps the inner
/// services of a customer counting for that customer, this holds for customer children too: their travel time, briefing
/// and corrections are billed but never counted as hours.
/// </summary>

using System.Linq.Expressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Schedules;

public static class WorkChangeOwnHours
{
    public static readonly Expression<Func<WorkChange, bool>> Counts =
        wc => wc.Work!.ParentWorkId == null || !CorrectionWorkChangeTypes.All.Contains(wc.Type);
}
