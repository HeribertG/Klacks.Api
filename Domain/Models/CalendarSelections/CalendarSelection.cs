// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Models.CalendarSelections;

public class CalendarSelection : BaseEntity
{
    public string Name { get; set; } = string.Empty;

    public List<SelectedCalendar> SelectedCalendars { get; set; } = new();
}
