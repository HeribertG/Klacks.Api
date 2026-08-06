// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports contracts whose scheduling rule belongs to an industry that is no longer active. Mirrors
/// the semantics of <see cref="IActiveIndustriesProvider"/>: no setting means no filtering at all,
/// the custom marker means only industry-less rules are active, and a slug list means everything
/// outside it is inactive. Rules carrying no industry tag are customer-owned and never reported.
/// </summary>
/// <param name="context">Read-only access to contracts, their rules and their client links</param>
/// <param name="activeIndustriesProvider">Supplies the currently active industry slugs</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Interfaces.Scheduling;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Scheduling;

public class IndustryMigrationReader : IIndustryMigrationReader
{
    private readonly DataBaseContext _context;
    private readonly IActiveIndustriesProvider _activeIndustriesProvider;

    public IndustryMigrationReader(
        DataBaseContext context,
        IActiveIndustriesProvider activeIndustriesProvider)
    {
        _context = context;
        _activeIndustriesProvider = activeIndustriesProvider;
    }

    public async Task<IReadOnlyList<IndustryMigrationCandidate>> GetContractsOnInactiveIndustriesAsync(
        CancellationToken cancellationToken = default)
    {
        var activeSlugs = await _activeIndustriesProvider.GetActiveIndustrySlugsAsync();
        if (activeSlugs == null)
        {
            return [];
        }

        var assignments = await _context.Contract
            .AsNoTracking()
            .Where(c => !c.IsDeleted && c.SchedulingRuleId.HasValue)
            .Join(
                _context.SchedulingRules.AsNoTracking().Where(r => !r.IsDeleted && r.Industry != string.Empty),
                contract => contract.SchedulingRuleId!.Value,
                rule => rule.Id,
                (contract, rule) => new
                {
                    contract.Id,
                    ContractName = contract.Name,
                    RuleId = rule.Id,
                    RuleName = rule.Name,
                    rule.Industry,
                })
            .ToListAsync(cancellationToken);

        var inactive = assignments
            .Where(a => !activeSlugs.Contains(a.Industry.ToLowerInvariant()))
            .ToList();

        if (inactive.Count == 0)
        {
            return [];
        }

        var contractIds = inactive.Select(a => a.Id).ToList();
        var clientCounts = await _context.ClientContract
            .AsNoTracking()
            .Where(cc => contractIds.Contains(cc.ContractId) && !cc.IsDeleted && cc.IsActive)
            .GroupBy(cc => cc.ContractId)
            .Select(g => new { ContractId = g.Key, Count = g.Count() })
            .ToDictionaryAsync(x => x.ContractId, x => x.Count, cancellationToken);

        return inactive
            .Select(a => new IndustryMigrationCandidate(
                a.Id,
                a.ContractName,
                a.RuleId,
                a.RuleName,
                a.Industry,
                clientCounts.TryGetValue(a.Id, out var count) ? count : 0))
            .OrderBy(c => c.ContractName)
            .ToList();
    }
}
