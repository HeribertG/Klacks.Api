// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Domain.Services.Schedules;

/// <summary>
/// The weekday rule of the effective contract data (the contract or, without one, the settings defaults): whether the
/// employee works on a weekday. One rule for the wizards, the harmonizer, the absence cover and the list of alternatives.
/// </summary>
public static class ContractWeekday
{
    /// <summary>True when the contract data lists the weekday as a working day.</summary>
    /// <param name="contract">Effective contract data of the employee</param>
    /// <param name="dayOfWeek">Weekday to judge</param>
    public static bool WorksOn(EffectiveContractData contract, DayOfWeek dayOfWeek) => dayOfWeek switch
    {
        DayOfWeek.Monday => contract.WorkOnMonday,
        DayOfWeek.Tuesday => contract.WorkOnTuesday,
        DayOfWeek.Wednesday => contract.WorkOnWednesday,
        DayOfWeek.Thursday => contract.WorkOnThursday,
        DayOfWeek.Friday => contract.WorkOnFriday,
        DayOfWeek.Saturday => contract.WorkOnSaturday,
        DayOfWeek.Sunday => contract.WorkOnSunday,
        _ => false,
    };
}
