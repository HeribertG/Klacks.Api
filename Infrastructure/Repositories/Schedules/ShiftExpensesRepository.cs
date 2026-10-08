// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core repository for shift default expenses with a batch query over several shifts.
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

    public async Task<List<ShiftExpenses>> GetByShiftIdsAsync(IEnumerable<Guid> shiftIds, CancellationToken cancellationToken = default)
    {
        var idList = shiftIds.Distinct().ToList();
        if (idList.Count == 0)
        {
            return [];
        }

        return await context.Set<ShiftExpenses>()
            .Where(e => idList.Contains(e.ShiftId))
            .OrderBy(e => e.CreateTime)
            .ToListAsync(cancellationToken);
    }
}
