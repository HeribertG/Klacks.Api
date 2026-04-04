// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for CalendarSelections with support for SelectedCalendars includes and state-based queries.
/// Delegates update logic to ICalendarSelectionUpdateService.
/// </summary>
/// <param name="context">Database context</param>
/// <param name="updateService">Service for updating CalendarSelections including SelectedCalendars</param>

using Klacks.Api.Domain.Common;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.CalendarSelections;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace Klacks.Api.Infrastructure.Repositories.CalendarSelections;

public class CalendarSelectionRepository : BaseRepository<CalendarSelection>, ICalendarSelectionRepository
{
    private readonly ICalendarSelectionUpdateService _updateService;

    public CalendarSelectionRepository(DataBaseContext context, ILogger<CalendarSelection> logger,
        ICalendarSelectionUpdateService updateService)
      : base(context, logger)
    {
        _updateService = updateService;
    }

    public override async Task<List<CalendarSelection>> List()
    {
        return await context.CalendarSelection
            .Include(cs => cs.SelectedCalendars)
            .ToListAsync();
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

    public async Task<int> CountActiveGroupsByCalendarSelectionAsync(Guid calendarSelectionId, CancellationToken cancellationToken = default)
    {
        return await context.Set<Group>()
            .CountAsync(g => !g.IsDeleted && g.CalendarSelectionId == calendarSelectionId, cancellationToken);
    }

    public async Task<int> CountActiveContractsByCalendarSelectionAsync(Guid calendarSelectionId, CancellationToken cancellationToken = default)
    {
        return await context.Set<Contract>()
            .CountAsync(c => !c.IsDeleted && c.CalendarSelectionId == calendarSelectionId, cancellationToken);
    }

    public async Task<List<Guid>> GetIdsByStateAsync(string country, string state, CancellationToken cancellationToken = default)
    {
        return await context.CalendarSelection
            .Where(cs => !cs.IsDeleted && cs.SelectedCalendars.Any(sc => sc.Country == country && sc.State == state))
            .Select(cs => cs.Id)
            .ToListAsync(cancellationToken);
    }
}
