// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for CRUD operations on shift default expenses.
/// </summary>
/// <param name="shiftId">Filter expenses by shift ID</param>
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IShiftExpensesRepository : IBaseRepository<ShiftExpenses>
{
    Task<List<ShiftExpenses>> GetByShiftId(Guid shiftId);
}
