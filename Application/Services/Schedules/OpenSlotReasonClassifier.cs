// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names the static cause of an open shift slot from the contracts of the employees in scope: nobody in scope, nobody
/// with an active contract that day, nobody who works that weekday, nobody who performs shift work although the shift
/// is not an early one. When every check passes the slot stays open because of capacity or planning rules, which only
/// the planner run can tell apart.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public static class OpenSlotReasonClassifier
{
    /// <summary>
    /// Classifies one open slot.
    /// </summary>
    /// <param name="contractsOnDate">Effective contract data of every employee in scope on the slot's date.</param>
    /// <param name="weekday">Weekday of the slot.</param>
    /// <param name="isEarlyShift">True when the shift is an early shift; only those may go to employees without shift work.</param>
    public static string Classify(IReadOnlyCollection<EffectiveContractData> contractsOnDate, DayOfWeek weekday, bool isEarlyShift)
    {
        if (contractsOnDate.Count == 0)
        {
            return ScenarioSummaryReasonCodes.NoAgentInScope;
        }

        var active = contractsOnDate.Where(c => c.HasActiveContract).ToList();
        if (active.Count == 0)
        {
            return ScenarioSummaryReasonCodes.NoActiveContract;
        }

        var working = active.Where(c => WorksOn(c, weekday)).ToList();
        if (working.Count == 0)
        {
            return ScenarioSummaryReasonCodes.NoAgentWorksOnWeekday;
        }

        if (!isEarlyShift && working.All(c => !c.PerformsShiftWork))
        {
            return ScenarioSummaryReasonCodes.NoAgentPerformsShiftWork;
        }

        return ScenarioSummaryReasonCodes.CapacityOrRules;
    }

    private static bool WorksOn(EffectiveContractData data, DayOfWeek weekday) => weekday switch
    {
        DayOfWeek.Monday => data.WorkOnMonday,
        DayOfWeek.Tuesday => data.WorkOnTuesday,
        DayOfWeek.Wednesday => data.WorkOnWednesday,
        DayOfWeek.Thursday => data.WorkOnThursday,
        DayOfWeek.Friday => data.WorkOnFriday,
        DayOfWeek.Saturday => data.WorkOnSaturday,
        DayOfWeek.Sunday => data.WorkOnSunday,
        _ => false,
    };
}
