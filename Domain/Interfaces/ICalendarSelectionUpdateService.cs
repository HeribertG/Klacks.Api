using Klacks.Api.Domain.Models.CalendarSelections;

namespace Klacks.Api.Domain.Interfaces;

public interface ICalendarSelectionUpdateService
{
    Task UpdateCalendarSelectionAsync(CalendarSelection existingCalendarSelection, CalendarSelection updatedModel);
    Task<CalendarSelection> GetWithSelectedCalendarsAsync(Guid id);
}