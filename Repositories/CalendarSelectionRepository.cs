using Klacks.Api.Datas;
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

    public async Task<CalendarSelection?> GetWithSelectedCalendars(Guid id)
    {
        return await context.CalendarSelection
                          .Include(c => c.SelectedCalendars)
                          .FirstOrDefaultAsync(c => c.Id == id);
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
    }
}
