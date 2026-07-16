// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

/// <summary>
/// Default implementation of <see cref="IWizardWarmStartBuilder"/>. Reads the real works of the period
/// preceding the target period directly from the DbContext and maps them weekday-true onto the target
/// period via <see cref="WarmStartWeekdayMapper"/>. An empty previous period yields an empty list.
/// </summary>
/// <param name="context">EF Core database context</param>
/// <param name="periodHoursService">Provider used to resolve the previous period's boundaries</param>
public sealed class WizardWarmStartBuilder : IWizardWarmStartBuilder
{
    private readonly DataBaseContext _context;
    private readonly IPeriodHoursService _periodHoursService;

    public WizardWarmStartBuilder(DataBaseContext context, IPeriodHoursService periodHoursService)
    {
        _context = context;
        _periodHoursService = periodHoursService;
    }

    public async Task<IReadOnlyList<CoreWarmStartAssignment>> BuildAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly periodFrom,
        DateOnly periodUntil,
        CancellationToken ct)
    {
        if (agentIds.Count == 0)
        {
            return [];
        }

        var (previousFrom, previousUntil) = await _periodHoursService
            .GetPeriodBoundariesAsync(periodFrom.AddDays(-1));

        var agentIdList = agentIds.ToList();

        // Always the real ledger (AnalyseToken == null), even for scenario runs: only accepted works are a confirmed pattern.
        var rawWorks = await _context.Work
            .AsNoTracking()
            .Where(w => agentIdList.Contains(w.ClientId)
                        && w.CurrentDate >= previousFrom
                        && w.CurrentDate <= previousUntil
                        && w.AnalyseToken == null)
            .Select(w => new WarmStartSourceWork(
                w.ClientId,
                w.CurrentDate,
                w.StartTime,
                w.EndTime,
                w.WorkTime,
                w.ShiftId))
            .ToListAsync(ct);

        if (rawWorks.Count == 0)
        {
            return [];
        }

        return WarmStartWeekdayMapper.Map(rawWorks, previousFrom, previousUntil, periodFrom, periodUntil);
    }
}
