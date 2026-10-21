// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Lists the task shifts that can be added to one slot of a container template: the tasks the schedule engine runs on
/// every day the slot applies on (ShiftDayActivityFilter), minus tasks already used by another container.
/// </summary>
/// <param name="ContainerId">Container shift whose template slot is filled</param>
/// <param name="Weekday">Stored weekday of the slot (0=Sunday .. 6=Saturday); only a storage key for the generic holiday slot</param>
/// <param name="FromTime">Start of the slot's time window</param>
/// <param name="UntilTime">End of the slot's time window</param>
/// <param name="SearchString">Optional filter on name, abbreviation and description</param>
/// <param name="ExcludeContainerId">Container whose own items still count as available</param>
/// <param name="IsHoliday">Slot flag: false or null = the normal slot of Weekday (every task ticked on Weekday, whatever its
/// holiday flags), true = a holiday slot (see IsWeekdayAndHoliday)</param>
/// <param name="IsWeekdayAndHoliday">Only read with IsHoliday: true = the holiday slot of Weekday (IsHoliday tasks and the
/// Weekday tasks with IsWeekdayAndHoliday), false or null = the generic holiday slot of every public holiday (IsHoliday
/// tasks and IsWeekdayAndHoliday tasks ticked on all seven weekdays)</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ContainerTemplates;

public record GetAvailableTasksQuery(
    Guid ContainerId,
    int Weekday,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    string? SearchString = null,
    Guid? ExcludeContainerId = null,
    bool? IsHoliday = null,
    bool? IsWeekdayAndHoliday = null
) : IRequest<List<ShiftResource>>;
