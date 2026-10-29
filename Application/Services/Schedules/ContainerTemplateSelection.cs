// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Picks the template a container skill without a holiday parameter edits for a weekday: the ordinary weekday
/// template, otherwise any template of that weekday (a holiday variant), otherwise none.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerTemplateSelection
{
    /// <param name="storageWeekday">Stored weekday 0=Sunday..6=Saturday</param>
    public static ContainerTemplateResource? ForWeekday(IEnumerable<ContainerTemplateResource> templates, int storageWeekday)
    {
        var ofWeekday = templates.Where(t => t.Weekday == storageWeekday).ToList();
        return ofWeekday.FirstOrDefault(t => !t.IsHoliday) ?? ofWeekday.FirstOrDefault();
    }
}
