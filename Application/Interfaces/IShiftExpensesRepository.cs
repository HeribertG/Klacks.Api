// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Repository for CRUD operations on shift default expenses.
/// </summary>
/// <param name="shiftIds">Filter expenses by several shift IDs in one query</param>
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftExpensesRepository : IBaseRepository<ShiftExpenses>
{
    Task<List<ShiftExpenses>> GetByShiftIdsAsync(IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default);
}
