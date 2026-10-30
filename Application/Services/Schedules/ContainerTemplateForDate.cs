// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule which template of a container applies to a calendar date: the ordinary template of the date's
/// weekday, otherwise any template of that weekday (a holiday variant), otherwise none. Public holidays are not
/// consulted. ContainerWorkExpansionService expands a container work by exactly this pick, and assign_container_week
/// decides with it which dates of a range have a template to expand.
/// </summary>

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerTemplateForDate
{
    /// <param name="templates">The templates of one container</param>
    /// <param name="date">The calendar date the container is booked on</param>
    public static ContainerTemplate? Pick(IEnumerable<ContainerTemplate> templates, DateOnly date)
    {
        var storageWeekday = (int)date.DayOfWeek;
        var ofWeekday = templates.Where(t => t.Weekday == storageWeekday).ToList();
        return ofWeekday.FirstOrDefault(t => !t.IsHoliday) ?? ofWeekday.FirstOrDefault();
    }
}
