// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Builds the container (ShiftType.IsContainer) the container-building skills persist through
/// IShiftRepository.AddWithSealedOrderHandling: one row in the shape of ContainerShiftRules, status OriginalShift and no
/// OriginalId, for a time frame, active on the given ISO weekdays, with one employee and quantity one, linked to the given
/// groups. A container has no order, so the row carries the templates and the works itself. The id is minted here, so a
/// skill calls it inside the transaction lambda and a retry builds a fresh container.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerShiftFactory
{
    private const int DefaultSumEmployees = 1;
    private const int DefaultQuantity = 1;

    /// <param name="spec">Name, time frame, validity, weekdays and groups of the container</param>
    public static Shift Build(ContainerShiftSpec spec)
    {
        var weekdays = WeekdayFlags.OfDays(spec.IsoWeekdays);

        return new Shift
        {
            Id = Guid.NewGuid(),
            Name = spec.Name,
            Abbreviation = spec.Abbreviation,
            Description = string.Empty,
            ClientId = null,
            MacroId = spec.MacroId,
            Status = ShiftStatus.OriginalShift,
            ShiftType = ShiftType.IsContainer,
            IsTimeRange = false,
            StartShift = spec.FromTime,
            EndShift = spec.UntilTime,
            FromDate = spec.FromDate,
            UntilDate = spec.UntilDate,
            WorkTime = spec.WorkTime,
            SumEmployees = DefaultSumEmployees,
            Quantity = DefaultQuantity,
            IsMonday = weekdays.Monday,
            IsTuesday = weekdays.Tuesday,
            IsWednesday = weekdays.Wednesday,
            IsThursday = weekdays.Thursday,
            IsFriday = weekdays.Friday,
            IsSaturday = weekdays.Saturday,
            IsSunday = weekdays.Sunday,
            GroupItems = spec.GroupItems,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = spec.UserName
        };
    }
}
