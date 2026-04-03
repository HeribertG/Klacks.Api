// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// EF Core repository for shift default expenses with GetByShiftId query.
/// </summary>
/// <param name="context">The database context</param>
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class ShiftExpensesRepository : BaseRepository<ShiftExpenses>, IShiftExpensesRepository
{
    public ShiftExpensesRepository(DataBaseContext context, ILogger<ShiftExpenses> logger)
        : base(context, logger)
    {
    }

    public async Task<List<ShiftExpenses>> GetByShiftId(Guid shiftId)
    {
        return await context.Set<ShiftExpenses>()
            .Where(e => e.ShiftId == shiftId)
            .OrderBy(e => e.CreateTime)
            .ToListAsync();
    }
}
