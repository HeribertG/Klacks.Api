// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Infrastructure.Repositories.CalendarSelections;

public class CalendarSelectionRepository : BaseRepository<CalendarSelection>, ICalendarSelectionRepository
{
    private readonly DataBaseContext context;
    private readonly ICalendarSelectionUpdateService _updateService;

    public CalendarSelectionRepository(DataBaseContext context, ILogger<CalendarSelection> logger,
        ICalendarSelectionUpdateService updateService)
      : base(context, logger)
    {
        this.context = context;
        _updateService = updateService;
    }

    public async Task<CalendarSelection?> GetWithSelectedCalendars(Guid id)
    {
        return await _updateService.GetWithSelectedCalendarsAsync(id);
    }

    public async Task Update(CalendarSelection model)
    {
        try
        {
            var existingCalendarSelection = await context.CalendarSelection
                .Include(cu => cu.SelectedCalendars)
                .SingleOrDefaultAsync(c => c.Id == model.Id);

            if (existingCalendarSelection == null)
            {
                Logger.LogWarning("Update: CalendarSelection with ID: {CalendarSelectionId} not found.", model.Id);
                throw new ValidationException($"The requested CalendarSelections was not found. {model.Name}!");
            }

            await _updateService.UpdateCalendarSelectionAsync(existingCalendarSelection, model);
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

    public async Task<List<Guid>> GetUsedByContractsAsync()
    {
        return await context.Contract
            .Where(c => c.CalendarSelectionId != null && !c.IsDeleted)
            .Select(c => c.CalendarSelectionId!.Value)
            .Distinct()
            .ToListAsync();
    }
}
