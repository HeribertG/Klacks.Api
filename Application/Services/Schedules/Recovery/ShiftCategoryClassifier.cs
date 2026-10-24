// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;
using Klacks.ScheduleRecovery.Model;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

/// <summary>
/// Classifies a shift as early, late or night by its time span, through the SAME inference Wizard 1 and Wizard 2 use,
/// so the recovery snapshot and the planner's manual candidate search sort a shift into the same kind.
/// </summary>
public static class ShiftCategoryClassifier
{
    /// <summary>Early, late or night for a shift running from <paramref name="start"/> to <paramref name="end"/>.</summary>
    public static ShiftCategory CategoryOf(TimeSpan start, TimeSpan end)
        => ShiftTypeInference.FromSpan(TimeOnly.FromTimeSpan(start), TimeOnly.FromTimeSpan(end)) switch
        {
            0 => ShiftCategory.Early,
            1 => ShiftCategory.Late,
            _ => ShiftCategory.Night,
        };
}
