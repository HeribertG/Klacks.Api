using Klacks.Api.Models.CalendarSelections;

namespace Klacks.Api.Interfaces;

public interface ICalendarSelectionRepository : IBaseRepository<CalendarSelection>
{
  Task Update(CalendarSelection model);
    Task<CalendarSelection?> GetWithSelectedCalendars(Guid id);
}
