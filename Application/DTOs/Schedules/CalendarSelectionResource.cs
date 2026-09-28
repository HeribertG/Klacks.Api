// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class CalendarSelectionResource
{
    public Guid Id { get; set; }

    public string Name { get; set; } = string.Empty;

    public bool IsSeeded { get; set; }

    public List<SelectedCalendarResource> SelectedCalendars { get; set; } = new();
}
