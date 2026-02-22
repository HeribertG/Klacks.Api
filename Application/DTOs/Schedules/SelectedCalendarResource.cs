// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

public class SelectedCalendarResource
{
    public Guid CalendarSelectionId { get; set; }

    public string Country { get; set; } = string.Empty;

    public Guid Id { get; set; }

    public string State { get; set; } = string.Empty;
}
