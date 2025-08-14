using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Domain.Services.CalendarSelections;

public class CalendarSelectionUpdateService : ICalendarSelectionUpdateService
{
    private readonly DataBaseContext _context;
    private readonly ILogger<CalendarSelectionUpdateService> _logger;

    public CalendarSelectionUpdateService(
        DataBaseContext context,
        ILogger<CalendarSelectionUpdateService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CalendarSelection> GetWithSelectedCalendarsAsync(Guid id)
    {
        _logger.LogInformation("Fetching CalendarSelection with ID: {CalendarSelectionId}", id);
        
        var calendarSelection = await _context.CalendarSelection
            .Include(c => c.SelectedCalendars)
            .FirstOrDefaultAsync(c => c.Id == id);

        if (calendarSelection == null)
        { 
            _logger.LogWarning("CalendarSelection with ID: {CalendarSelectionId} not found.", id);
            throw new ValidationException($"CalendarSelection with ID {id} not found.");
        }

        _logger.LogInformation("CalendarSelection with ID: {CalendarSelectionId} found successfully.", id);
        return calendarSelection;
    }

    public async Task UpdateCalendarSelectionAsync(CalendarSelection existingCalendarSelection, CalendarSelection updatedModel)
    {
        _logger.LogInformation("Updating CalendarSelection with ID: {CalendarSelectionId}", updatedModel.Id);

        UpdateBasicProperties(existingCalendarSelection, updatedModel);
        await UpdateSelectedCalendarsAsync(existingCalendarSelection, updatedModel);

        _logger.LogInformation("CalendarSelection with ID: {CalendarSelectionId} updated successfully.", updatedModel.Id);
    }

    private void UpdateBasicProperties(CalendarSelection existing, CalendarSelection updated)
    {
        existing.Name = updated.Name;
    }

    private async Task UpdateSelectedCalendarsAsync(CalendarSelection existing, CalendarSelection updated)
    {
        var updatedKeys = updated.SelectedCalendars
            .Select(sc => $"{sc.Country}|{sc.State}")
            .ToHashSet();

        RemoveUnusedSelectedCalendars(existing, updatedKeys);
        AddNewSelectedCalendars(existing, updated);

        await _context.SaveChangesAsync();
    }

    private void RemoveUnusedSelectedCalendars(CalendarSelection existing, HashSet<string> updatedKeys)
    {
        existing.SelectedCalendars.RemoveAll(sc =>
            !updatedKeys.Contains($"{sc.Country}|{sc.State}"));
    }

    private void AddNewSelectedCalendars(CalendarSelection existing, CalendarSelection updated)
    {
        foreach (var updatedSelectedCalendar in updated.SelectedCalendars)
        {
            var existingSelectedCalendar = existing.SelectedCalendars
                .FirstOrDefault(sc =>
                    sc.Country == updatedSelectedCalendar.Country &&
                    sc.State == updatedSelectedCalendar.State);

            if (existingSelectedCalendar == null)
            {
                existing.SelectedCalendars.Add(updatedSelectedCalendar);
            }
        }
    }
}