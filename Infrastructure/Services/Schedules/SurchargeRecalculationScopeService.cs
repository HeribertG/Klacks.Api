// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the client scope and date window affected by a retroactive contract or scheduling-rule
/// change. Only real-mode works (AnalyseToken null) are considered: automatic recalculation triggers
/// target persisted productive data, scenario rows have their own lifecycle.
/// </summary>
/// <param name="context">Database access for contracts, client-contract assignments and works</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

public class SurchargeRecalculationScopeService : ISurchargeRecalculationScope
{
    private readonly DataBaseContext _context;

    public SurchargeRecalculationScopeService(DataBaseContext context)
    {
        _context = context;
    }

    public Task<List<Guid>> GetClientIdsForContractAsync(Guid contractId, CancellationToken cancellationToken = default)
    {
        return _context.ClientContract
            .Where(cc => !cc.IsDeleted && cc.ContractId == contractId)
            .Select(cc => cc.ClientId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public Task<List<ContractRecalculationWindow>> GetContractWindowsForRulesAsync(IReadOnlyCollection<Guid> ruleIds, CancellationToken cancellationToken = default)
    {
        return _context.Contract
            .Where(c => !c.IsDeleted && c.SchedulingRuleId.HasValue && ruleIds.Contains(c.SchedulingRuleId.Value))
            .Select(c => new ContractRecalculationWindow(
                c.Id,
                DateOnly.FromDateTime(c.ValidFrom),
                c.ValidUntil.HasValue ? DateOnly.FromDateTime(c.ValidUntil.Value) : null))
            .ToListAsync(cancellationToken);
    }

    public Task<List<Guid>> GetClientIdsForContractsAsync(IReadOnlyCollection<Guid> contractIds, CancellationToken cancellationToken = default)
    {
        return _context.ClientContract
            .Where(cc => !cc.IsDeleted && contractIds.Contains(cc.ContractId))
            .Select(cc => cc.ClientId)
            .Distinct()
            .ToListAsync(cancellationToken);
    }

    public async Task<DateOnly?> GetLatestWorkDateAsync(IReadOnlyCollection<Guid> clientIds, DateOnly from, CancellationToken cancellationToken = default)
    {
        return await _context.Work
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.CurrentDate >= from
                && clientIds.Contains(w.ClientId))
            .Select(w => (DateOnly?)w.CurrentDate)
            .MaxAsync(cancellationToken);
    }

    public Task<bool> HasWorksInWindowAsync(IReadOnlyCollection<Guid> clientIds, DateOnly from, DateOnly until, CancellationToken cancellationToken = default)
    {
        return _context.Work
            .AnyAsync(
                w => !w.IsDeleted
                    && w.AnalyseToken == null
                    && w.CurrentDate >= from
                    && w.CurrentDate <= until
                    && clientIds.Contains(w.ClientId),
                cancellationToken);
    }

    public async Task<WorkRecalculationWindow?> GetUnlockedRealWorkWindowAsync(CancellationToken cancellationToken = default)
    {
        var window = await _context.Work
            .Where(w => !w.IsDeleted
                && w.AnalyseToken == null
                && w.LockLevel == WorkLockLevel.None)
            .GroupBy(_ => 1)
            .Select(g => new { From = g.Min(w => w.CurrentDate), Until = g.Max(w => w.CurrentDate) })
            .FirstOrDefaultAsync(cancellationToken);

        return window is null ? null : new WorkRecalculationWindow(window.From, window.Until);
    }
}
