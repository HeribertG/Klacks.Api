// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public class SelectedCalendarResource
{
    public Guid CalendarSelectionId { get; set; }

    public string Country { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string State { get; set; } = string.Empty;

    public bool? OfficialOverride { get; set; }
}
