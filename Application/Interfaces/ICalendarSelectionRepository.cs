using Klacks.Api.Models.CalendarSelections;

namespace Klacks.Api.Application.Interfaces;

public interface ICalendarSelectionRepository : IBaseRepository<CalendarSelection>
{
    Task Update(CalendarSelection model);
    Task<CalendarSelection?> GetWithSelectedCalendars(Guid id);
}
