// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Creates a deep, identity-free copy of a Shift aggregate including its group items and expenses.
/// </summary>

using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.Shifts;

public static class ShiftCloner
{
    public static Shift Clone(Shift source)
    {
        return new Shift
        {
            Id = Guid.Empty,
            CuttingAfterMidnight = source.CuttingAfterMidnight,
            Abbreviation = source.Abbreviation,
            Description = source.Description,
            MacroId = source.MacroId,
            Name = source.Name,
            Status = source.Status,
            AfterShift = source.AfterShift,
            BeforeShift = source.BeforeShift,
            EndShift = source.EndShift,
            FromDate = source.FromDate,
            StartShift = source.StartShift,
            UntilDate = source.UntilDate,
            BriefingTime = source.BriefingTime,
            DebriefingTime = source.DebriefingTime,
            TravelTimeAfter = source.TravelTimeAfter,
            TravelTimeBefore = source.TravelTimeBefore,
            IsFriday = source.IsFriday,
            IsHoliday = source.IsHoliday,
            IsMonday = source.IsMonday,
            IsSaturday = source.IsSaturday,
            IsSunday = source.IsSunday,
            IsThursday = source.IsThursday,
            IsTuesday = source.IsTuesday,
            IsWednesday = source.IsWednesday,
            IsWeekdayAndHoliday = source.IsWeekdayAndHoliday,
            IsSporadic = source.IsSporadic,
            SporadicScope = source.SporadicScope,
            IsTimeRange = source.IsTimeRange,
            Quantity = source.Quantity,
            SumEmployees = source.SumEmployees,
            WorkTime = source.WorkTime,
            ShiftType = source.ShiftType,
            OriginalId = source.OriginalId,
            ParentId = source.ParentId,
            RootId = source.RootId,
            Lft = source.Lft,
            Rgt = source.Rgt,
            ClientId = source.ClientId,
            GroupItems = source.GroupItems.Select(gi => new GroupItem
            {
                Id = Guid.Empty,
                GroupId = gi.GroupId,
                ValidFrom = gi.ValidFrom,
                ValidUntil = gi.ValidUntil,
                ShiftId = Guid.Empty
            }).ToList(),
            ShiftExpenses = source.ShiftExpenses.Select(e => new ShiftExpenses
            {
                Id = Guid.Empty,
                Amount = e.Amount,
                Description = e.Description,
                Taxable = e.Taxable,
                ShiftId = Guid.Empty
            }).ToList()
        };
    }
}
