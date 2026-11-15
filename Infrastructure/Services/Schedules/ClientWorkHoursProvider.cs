// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default <see cref="IClientWorkHoursProvider"/>: groups a client's Work rows by CurrentDate and sums
/// WorkTime. Reads the rows a rule check sees (ScenarioRuleRows: null = real plan, a token = the scenario plus the real
/// works it does not carry); soft-deleted rows are excluded by the query. A container
/// child of the container's own person adds nothing, because the container counts its time (WorkOwnHours).
/// </summary>
/// <param name="context">Database access for the client's Work rows</param>
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Klacks.Api.Domain.Services.Schedules;

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
        var rows = await ScenarioRuleRows.Works(_context, analyseToken)
            .Where(w => w.ClientId == clientId
                && w.CurrentDate >= start
                && w.CurrentDate <= end)
            .Where(WorkOwnHours.Counts)
            .GroupBy(w => w.CurrentDate)
            .Select(g => new { Date = g.Key, Hours = g.Sum(w => w.WorkTime) })
            .ToListAsync();

        return rows.ToDictionary(r => r.Date, r => r.Hours);
    }
}
