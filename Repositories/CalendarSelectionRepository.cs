using Klacks_api.Datas;
using Klacks_api.Interfaces;
using Klacks_api.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore;

namespace Klacks_api.Repositories
{
  public class CalendarSelectionRepository : BaseRepository<CalendarSelection>, ICalendarSelectionRepository
  {
    private readonly DataBaseContext context;

    public CalendarSelectionRepository(DataBaseContext context)
      : base(context)
    {
      this.context = context;
    }

    public async Task Update(CalendarSelection model)
    {
      var existingCalendarSelection = await context.CalendarSelection.Include(cu => cu.SelectedCalendars)
                                                                    .SingleOrDefaultAsync(c => c.Id == model.Id);

      if (existingCalendarSelection == null)
      {
        throw new RepositoryException($"The requested CalendarSelections was not found. {model.Name}!");
      }

      existingCalendarSelection.Name = model.Name;

      var updatedSelectedCalendarIds = model.SelectedCalendars.Select(sc => sc.Id).ToList();

      // Remove calendars that are not in the update model
      existingCalendarSelection.SelectedCalendars.RemoveAll(sc => !updatedSelectedCalendarIds.Contains(sc.Id));

      foreach (var updatedSelectedCalendar in model.SelectedCalendars)
      {
        var existingSelectedCalendar = existingCalendarSelection.SelectedCalendars
                                                                 .FirstOrDefault(sc => sc.Id == updatedSelectedCalendar.Id);

        if (existingSelectedCalendar == null)
        {
          // Add new selected calendar
          existingCalendarSelection.SelectedCalendars.Add(updatedSelectedCalendar);
        }
        else
        {
          // Update existing selected calendar properties
          existingSelectedCalendar.Country = updatedSelectedCalendar.Country;
          existingSelectedCalendar.State = updatedSelectedCalendar.State;
        }
      }

      try
      {
        await context.SaveChangesAsync();
      }
      catch (DbUpdateConcurrencyException)
      {
        throw new RepositoryException("Concurrency conflict occurred while updating the CalendarSelections. Please reload and try again.");
      }
      catch (Exception)
      {
        throw new RepositoryException("An unexpected error occurred while updating the CalendarSelections.");
      }
    }
  }
}
