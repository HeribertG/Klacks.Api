// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names the static cause of an open shift slot from the effective contract data of the employees in scope who are
/// company members on the slot's date: nobody in scope (or nobody a member that day), nobody who works that weekday,
/// nobody who performs shift work although the shift is not an early one. An employee without an active contract that
/// day is a candidate with the settings defaults the contract provider returns. When every check passes the slot stays
/// open because of capacity or planning rules, which only the planner run can tell apart.
/// </summary>

using Klacks.Api.Application.Constants;
using Klacks.Api.Domain.Models.Associations;
using Klacks.ScheduleOptimizer.TokenEvolution.Initialization;

namespace Klacks.Api.Application.Services.Schedules;

public static class OpenSlotReasonClassifier
{
    /// <summary>
    /// Classifies one open slot.
    /// </summary>
    /// <param name="contractsOnDate">Effective contract data (contract or settings defaults) of every employee in scope who is a member on the slot's date.</param>
    /// <param name="weekday">Weekday of the slot.</param>
    /// <param name="shiftTypeIndex">Shift type of the slot (ShiftTypeInference); <see cref="ShiftWorkRule"/> decides who may take it.</param>
    public static string Classify(IReadOnlyCollection<EffectiveContractData> contractsOnDate, DayOfWeek weekday, int shiftTypeIndex)
    {
        if (contractsOnDate.Count == 0)
        {
            return ScenarioSummaryReasonCodes.NoAgentInScope;
        }

        var working = contractsOnDate.Where(c => WorksOn(c, weekday)).ToList();
        if (working.Count == 0)
        {
            return ScenarioSummaryReasonCodes.NoAgentWorksOnWeekday;
        }

        if (!working.Any(c => ShiftWorkRule.Permits(c.PerformsShiftWork, shiftTypeIndex)))
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
