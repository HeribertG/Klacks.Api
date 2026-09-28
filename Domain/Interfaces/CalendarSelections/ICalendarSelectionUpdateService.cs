// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.CalendarSelections;

namespace Klacks.Api.Domain.Interfaces.CalendarSelections;

public interface ICalendarSelectionUpdateService
{
    Task UpdateCalendarSelectionAsync(CalendarSelection existingCalendarSelection, CalendarSelection updatedModel);
    Task<CalendarSelection> GetWithSelectedCalendarsAsync(Guid id);
}