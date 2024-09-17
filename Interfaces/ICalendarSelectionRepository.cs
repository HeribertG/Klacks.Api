using Klacks_api.Models.CalendarSelections;

namespace Klacks_api.Interfaces;

public interface ICalendarSelectionRepository : IBaseRepository<CalendarSelection>
{
  Task Update(CalendarSelection model);
}
