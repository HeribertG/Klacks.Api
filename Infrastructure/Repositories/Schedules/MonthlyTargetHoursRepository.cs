// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Repository for the company-wide monthly target hours table. Rows are sparse, so a year may
/// contain anything from zero to twelve rows.
/// </summary>

using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Schedules;

public class MonthlyTargetHoursRepository : BaseRepository<MonthlyTargetHours>, IMonthlyTargetHoursRepository
{
    public MonthlyTargetHoursRepository(DataBaseContext context, ILogger<MonthlyTargetHours> logger)
        : base(context, logger)
    {
    }

    public override async Task<List<MonthlyTargetHours>> List()
    {
        return await context.MonthlyTargetHours
            .AsNoTracking()
            .OrderBy(m => m.Year)
            .ThenBy(m => m.Month)
            .ToListAsync();
    }

    public async Task<List<MonthlyTargetHours>> ListByYear(int year)
    {
        return await context.MonthlyTargetHours
            .Where(m => m.Year == year)
            .AsNoTracking()
            .OrderBy(m => m.Month)
            .ToListAsync();
    }

    public async Task<MonthlyTargetHours?> GetByYearMonth(int year, int month)
    {
        return await context.MonthlyTargetHours
            .AsNoTracking()
            .FirstOrDefaultAsync(m => m.Year == year && m.Month == month);
    }

    public override async Task<MonthlyTargetHours?> Put(MonthlyTargetHours model)
    {
        var existing = await context.MonthlyTargetHours.FirstOrDefaultAsync(m => m.Id == model.Id);

        if (existing == null)
        {
            Logger.LogWarning("MonthlyTargetHours not found for update: {MonthlyTargetHoursId}", model.Id);
            return null;
        }

        existing.Year = model.Year;
        existing.Month = model.Month;
        existing.Hours = model.Hours;

        Logger.LogInformation("MonthlyTargetHours updated: {Year}-{Month}, hours {Hours}",
            existing.Year, existing.Month, existing.Hours);

        return existing;
    }
}
