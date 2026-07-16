// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IClientWorkHoursProvider"/>: groups a client's Work rows by CurrentDate and sums
/// WorkTime. Uses the same AnalyseToken equality semantics as OvertimeSurchargeCalculator's period
/// lookup (exact token match, null = real mode); soft-deleted rows are excluded by the EF query filter.
/// </summary>
/// <param name="context">Database access for the client's Work rows</param>
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class ClientWorkHoursProvider : IClientWorkHoursProvider
{
    private readonly DataBaseContext _context;

    public ClientWorkHoursProvider(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<IReadOnlyDictionary<DateOnly, decimal>> GetWorkHoursByDayAsync(
        Guid clientId,
        DateOnly start,
        DateOnly end,
        Guid? analyseToken)
    {
        var rows = await _context.Work
            .Where(w => w.ClientId == clientId
                && w.AnalyseToken == analyseToken
                && w.CurrentDate >= start
                && w.CurrentDate <= end)
            .GroupBy(w => w.CurrentDate)
            .Select(g => new { Date = g.Key, Hours = g.Sum(w => w.WorkTime) })
            .ToListAsync();

        return rows.ToDictionary(r => r.Date, r => r.Hours);
    }
}
