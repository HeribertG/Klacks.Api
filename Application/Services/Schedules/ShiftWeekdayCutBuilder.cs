// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Cuts a plannable shift that runs on several weekdays into one part per weekday, the way the Ui cuts by weekday, with
/// the same machinery as every cut (ShiftCutFacade). The plannable shift itself is reduced to its first weekday (an update
/// that turns it into a SplitShift), every other weekday becomes a new SplitShift part cut from the sealed order. Each part
/// is a clone of the plannable shift (ShiftCutPartResourceFactory) whose only difference is its weekday flags. Part ids are
/// minted here, so a caller that plans inside a transaction lambda gets fresh ids on a retry.
/// </summary>

using Klacks.Api.Application.Commands.Shifts;
using Klacks.Api.Application.DTOs.Associations;
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class ShiftWeekdayCutBuilder
{
    /// <param name="shift">The plannable shift</param>
    public static IReadOnlyList<int> ActiveIsoWeekdays(Shift shift) => WeekdayFlags.Of(shift).IsoDays();

    /// <param name="source">The plannable shift to cut; it must run on at least two weekdays</param>
    /// <param name="sealedOrderId">The sealed order the parts descend from</param>
    /// <param name="groups">The groups of the plannable shift with their validity, carried by every part</param>
    public static ShiftWeekdayCutPlan Build(Shift source, Guid sealedOrderId, List<SimpleGroupResource> groups)
    {
        var parts = ActiveIsoWeekdays(source)
            .Select((day, index) => new ShiftWeekdayCutPart(
                index == 0 ? source.Id : Guid.NewGuid(), source.Id, day, index == 0))
            .ToList();

        var operations = parts
            .Select(part => new CutOperation
            {
                Type = part.Converted ? CutOperation.UpdateType : CutOperation.CreateType,
                ParentId = sealedOrderId.ToString(),
                Data = BuildPartResource(source, sealedOrderId, part, groups)
            })
            .ToList();

        return new ShiftWeekdayCutPlan(source, sealedOrderId, parts, operations);
    }

    private static ShiftResource BuildPartResource(
        Shift source, Guid sealedOrderId, ShiftWeekdayCutPart part, List<SimpleGroupResource> groups)
    {
        var resource = ShiftCutPartResourceFactory.CopyOf(source, part.Id, sealedOrderId, groups);
        var flags = WeekdayFlags.OfDays([part.IsoWeekday]);
        resource.IsMonday = flags.Monday;
        resource.IsTuesday = flags.Tuesday;
        resource.IsWednesday = flags.Wednesday;
        resource.IsThursday = flags.Thursday;
        resource.IsFriday = flags.Friday;
        resource.IsSaturday = flags.Saturday;
        resource.IsSunday = flags.Sunday;
        return resource;
    }
}
