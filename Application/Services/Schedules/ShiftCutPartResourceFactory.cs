// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one place that turns a plannable shift into the resource of a cut part, for the time cut (ShiftTimeCutPlanner) and the
/// weekday cut (ShiftWeekdayCutBuilder). A part is a clone of the plannable shift with status SplitShift and the sealed order
/// as its origin, like the clone the Ui makes before it changes one dimension: customer, macro, type, dates, times (also the
/// briefing, debriefing and travel times), weekdays, holiday flags, time-range and sporadic flags, staff count, quantity and
/// the groups with their validity are carried over. The caller then overrides the one dimension it cuts. Default expenses
/// and required qualifications are not carried: ShiftCutFacade gives every new part fresh copies of its parent's rows.
/// </summary>

using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class ShiftCutPartResourceFactory
{
    /// <param name="source">The plannable shift the part is cut from</param>
    /// <param name="partId">Id of the part; the source's own id for the part that reuses the plannable shift</param>
    /// <param name="sealedOrderId">The sealed order the part descends from</param>
    /// <param name="groups">The groups the part is linked to, with their validity</param>
    public static ShiftResource CopyOf(Shift source, Guid partId, Guid sealedOrderId, List<SimpleGroupResource> groups)
    {
        return new ShiftResource
        {
            Id = partId,
            Name = source.Name,
            Abbreviation = source.Abbreviation,
            Description = source.Description,
            Status = ShiftStatus.SplitShift,
            ShiftType = source.ShiftType,
            OriginalId = sealedOrderId,
            ClientId = source.ClientId,
            MacroId = source.MacroId,
            StartShift = source.StartShift,
            EndShift = source.EndShift,
            BeforeShift = source.BeforeShift,
            AfterShift = source.AfterShift,
            BriefingTime = source.BriefingTime,
            DebriefingTime = source.DebriefingTime,
            TravelTimeBefore = source.TravelTimeBefore,
            TravelTimeAfter = source.TravelTimeAfter,
            FromDate = source.FromDate,
            UntilDate = source.UntilDate,
            WorkTime = source.WorkTime,
            CuttingAfterMidnight = source.CuttingAfterMidnight,
            SumEmployees = source.SumEmployees,
            Quantity = source.Quantity,
            IsMonday = source.IsMonday,
            IsTuesday = source.IsTuesday,
            IsWednesday = source.IsWednesday,
            IsThursday = source.IsThursday,
            IsFriday = source.IsFriday,
            IsSaturday = source.IsSaturday,
            IsSunday = source.IsSunday,
            IsHoliday = source.IsHoliday,
            IsWeekdayAndHoliday = source.IsWeekdayAndHoliday,
            IsTimeRange = source.IsTimeRange,
            IsSporadic = source.IsSporadic,
            SporadicScope = source.SporadicScope,
            Groups = groups
        };
    }
}
