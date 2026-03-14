// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for CalendarSelection CRUD and state-based lookups.
/// </summary>

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.CalendarSelections;

namespace Klacks.Api.Application.Interfaces;

public interface ICalendarSelectionRepository : IBaseRepository<CalendarSelection>
{
    Task Update(CalendarSelection model);
    Task<CalendarSelection?> GetWithSelectedCalendars(Guid id);
    Task<List<Guid>> GetUsedByContractsAsync();
    Task<int> CountActiveGroupsByCalendarSelectionAsync(Guid calendarSelectionId, CancellationToken cancellationToken = default);
    Task<int> CountActiveContractsByCalendarSelectionAsync(Guid calendarSelectionId, CancellationToken cancellationToken = default);
    Task<List<Guid>> GetIdsByStateAsync(string country, string state, CancellationToken cancellationToken = default);
}
