// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="IClientOnCallHoursProvider"/>: loads a client's OnCallPresence/OnCallStandby work
/// changes (joined to their parent Work for date, client and scenario scoping) and sums ChangeTime
/// weighted by the per-category factors of the supplied config. Soft-deleted work changes and works are
/// excluded by the EF query filter; scenario isolation follows the parent Work's AnalyseToken.
/// </summary>
/// <param name="context">Database access for the client's on-call work changes.</param>
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Scheduling;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class ClientOnCallHoursProvider : IClientOnCallHoursProvider
{
    private readonly DataBaseContext _context;

    public ClientOnCallHoursProvider(DataBaseContext context)
    {
        _context = context;
    }

    public async Task<decimal> GetWeightedOnCallHoursAsync(
        Guid clientId,
        DateOnly start,
        DateOnly end,
        Guid? analyseToken,
        OnCallConfig config)
    {
        if (!config.Enabled)
        {
            return 0m;
        }

        var rows = await _context.WorkChange
            .Where(wc => (wc.Type == WorkChangeType.OnCallPresence || wc.Type == WorkChangeType.OnCallStandby)
                && wc.Work!.ClientId == clientId
                && wc.Work.CurrentDate >= start
                && wc.Work.CurrentDate <= end
                && wc.Work.AnalyseToken == analyseToken)
            .Select(wc => new { wc.Type, wc.ChangeTime })
            .ToListAsync();

        return rows.Sum(r => r.ChangeTime * config.FactorFor(r.Type));
    }
}
