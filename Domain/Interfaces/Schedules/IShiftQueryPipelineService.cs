// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Facade-Interface fuer die Shift-Query-Pipeline (Status-, Datums-, Such-, Sortier- und Paginierungsfilter).
/// </summary>
/// <param name="query">Basis-Query auf die Filter angewendet werden</param>
/// <param name="filter">Enthält alle Filterkriterien (Status, Datumsbereich, Suchbegriff, Sortierung, Paginierung)</param>
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.DTOs.Filter;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IShiftQueryPipelineService
{
    IQueryable<Shift> ApplyStatusFilter(IQueryable<Shift> query, ShiftFilterType filterType, bool isSealedOrder = false, bool isTimeRange = true, bool isSporadic = true);

    IQueryable<Shift> ApplyDateRangeFilter(IQueryable<Shift> query, bool activeDateRange, bool formerDateRange, bool futureDateRange);

    IQueryable<Shift> ApplySearchFilter(IQueryable<Shift> query, string searchString, bool includeClient);

    IQueryable<Shift> ApplySorting(IQueryable<Shift> query, string orderBy, string sortOrder);

    Task<TruncatedShift> ApplyPaginationAsync(IQueryable<Shift> filteredQuery, ShiftFilter filter);
}
