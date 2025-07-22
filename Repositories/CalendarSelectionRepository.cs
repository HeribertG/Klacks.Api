using Klacks.Api.Datas;
using Klacks.Api.Exceptions;
using Klacks.Api.Interfaces;
using Klacks.Api.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Repositories;

public class CalendarSelectionRepository : BaseRepository<CalendarSelection>, ICalendarSelectionRepository
{
    private readonly DataBaseContext context;

    public CalendarSelectionRepository(DataBaseContext context, ILogger<CalendarSelection> logger)
      : base(context, logger)
    {
        this.context = context;
    }

    public async Task<CalendarSelection> GetWithSelectedCalendars(Guid id)
    {
        Logger.LogInformation("Fetching CalendarSelection with ID: {CalendarSelectionId}", id);
        var calendarSelection = await context.CalendarSelection
                          .Include(c => c.SelectedCalendars)
                          .FirstOrDefaultAsync(c => c.Id == id);

        if (calendarSelection == null)
        {
            Logger.LogWarning("CalendarSelection with ID: {CalendarSelectionId} not found.", id);
            throw new KeyNotFoundException($"CalendarSelection with ID {id} not found.");
        }

        Logger.LogInformation("CalendarSelection with ID: {CalendarSelectionId} found successfully.", id);
        return calendarSelection;
    }

    public async Task Update(CalendarSelection model)
    {
        Logger.LogInformation("Updating CalendarSelection with ID: {CalendarSelectionId}", model.Id);
        var existingCalendarSelection = await context.CalendarSelection.Include(cu => cu.SelectedCalendars)
                                                                     .SingleOrDefaultAsync(c => c.Id == model.Id);
        if (existingCalendarSelection == null)
        {
            Logger.LogWarning("Update: CalendarSelection with ID: {CalendarSelectionId} not found.", model.Id);
            throw new KeyNotFoundException($"The requested CalendarSelections was not found. {model.Name}!");
        }

        try
        {
            existingCalendarSelection.Name = model.Name;

            var updatedKeys = model.SelectedCalendars
                .Select(sc => $"{sc.Country}|{sc.State}")
                .ToHashSet();

            existingCalendarSelection.SelectedCalendars.RemoveAll(sc =>
                !updatedKeys.Contains($"{sc.Country}|{sc.State}"));

            foreach (var updatedSelectedCalendar in model.SelectedCalendars)
            {
                var existingSelectedCalendar = existingCalendarSelection.SelectedCalendars
                    .FirstOrDefault(sc =>
                        sc.Country == updatedSelectedCalendar.Country &&
                        sc.State == updatedSelectedCalendar.State);

                if (existingSelectedCalendar == null)
                {
                    existingCalendarSelection.SelectedCalendars.Add(updatedSelectedCalendar);
                }
            }

            await context.SaveChangesAsync();
            Logger.LogInformation("CalendarSelection with ID: {CalendarSelectionId} updated successfully.", model.Id);
        }
        catch (DbUpdateException ex)
        {
            Logger.LogError(ex, "Failed to update CalendarSelection with ID: {CalendarSelectionId}. Database update error.", model.Id);
            throw new InvalidRequestException($"Failed to update CalendarSelection with ID {model.Id} due to a database error.");
        }
        catch (Exception ex)
        {
            Logger.LogError(ex, "An unexpected error occurred while updating CalendarSelection with ID: {CalendarSelectionId}.", model.Id);
            throw;
        }
    }
}
