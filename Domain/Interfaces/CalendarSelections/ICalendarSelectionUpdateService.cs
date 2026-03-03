// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.CalendarSelections;

namespace Klacks.Api.Domain.Interfaces.CalendarSelections;

public interface ICalendarSelectionUpdateService
{
    Task UpdateCalendarSelectionAsync(CalendarSelection existingCalendarSelection, CalendarSelection updatedModel);
    Task<CalendarSelection> GetWithSelectedCalendarsAsync(Guid id);
}