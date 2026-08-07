// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Reports contracts whose scheduling rule belongs to an industry that is no longer active. Mirrors
/// the semantics of <see cref="IActiveIndustriesProvider"/>: no setting means no filtering at all,
/// the custom marker means only industry-less rules are active, and a slug list means everything
/// outside it is inactive. Rules carrying no industry tag are customer-owned and never reported.
/// Each reported contract also carries a read-only equivalence proposal: imported presets live under
/// the "region-setup:industryProfiles:{industry}:..." key namespace, so the same preset of another
/// industry differs only in that one segment. When exactly one rule of a currently active industry
/// shares the remaining key tail, it is offered as the target - nothing is ever reassigned here.
/// </summary>
/// <param name="context">Read-only access to contracts, their rules and their client links</param>
/// <param name="activeIndustriesProvider">Supplies the currently active industry slugs</param>

using Klacks.Api.Application.DTOs.Scheduling;
using Klacks.Api.Application.Interfaces.Scheduling;
using Klacks.Api.Domain.Constants;
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
                    rule.ImportSourceKey,
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

        var equivalents = await LoadUnambiguousActiveEquivalentsAsync(activeSlugs, cancellationToken);

        return inactive
            .Select(a =>
            {
                var suggestion = FindSuggestion(equivalents, a.ImportSourceKey);
                return new IndustryMigrationCandidate(
                    a.Id,
                    a.ContractName,
                    a.RuleId,
                    a.RuleName,
                    a.Industry,
                    clientCounts.TryGetValue(a.Id, out var count) ? count : 0,
                    suggestion?.Id,
                    suggestion?.Name);
            })
            .OrderBy(c => c.ContractName)
            .ToList();
    }

    private static (Guid Id, string Name)? FindSuggestion(
        IReadOnlyDictionary<string, (Guid Id, string Name)> equivalents,
        string importSourceKey)
    {
        var tail = ExtractIndustryProfileTail(importSourceKey);
        if (tail == null || !equivalents.TryGetValue(tail, out var match))
        {
            return null;
        }

        return match;
    }

    private async Task<IReadOnlyDictionary<string, (Guid Id, string Name)>> LoadUnambiguousActiveEquivalentsAsync(
        IReadOnlyCollection<string> activeSlugs,
        CancellationToken cancellationToken)
    {
        if (activeSlugs.Count == 0)
        {
            return new Dictionary<string, (Guid Id, string Name)>(StringComparer.Ordinal);
        }

        var importedRules = await _context.SchedulingRules
            .AsNoTracking()
            .Where(r => !r.IsDeleted
                        && r.Industry != string.Empty
                        && r.ImportSourceKey.StartsWith(RegionSetupImportKeys.IndustryProfilesPrefix))
            .Select(r => new { r.Id, r.Name, r.Industry, r.ImportSourceKey })
            .ToListAsync(cancellationToken);

        var byTail = new Dictionary<string, (Guid Id, string Name)>(StringComparer.Ordinal);
        var ambiguousTails = new HashSet<string>(StringComparer.Ordinal);

        foreach (var rule in importedRules)
        {
            if (!activeSlugs.Contains(rule.Industry.ToLowerInvariant()))
            {
                continue;
            }

            var tail = ExtractIndustryProfileTail(rule.ImportSourceKey);
            if (tail == null)
            {
                continue;
            }

            if (!byTail.TryAdd(tail, (rule.Id, rule.Name)))
            {
                ambiguousTails.Add(tail);
            }
        }

        foreach (var tail in ambiguousTails)
        {
            byTail.Remove(tail);
        }

        return byTail;
    }

    private static string? ExtractIndustryProfileTail(string importSourceKey)
    {
        if (!importSourceKey.StartsWith(RegionSetupImportKeys.IndustryProfilesPrefix, StringComparison.Ordinal))
        {
            return null;
        }

        var afterPrefix = importSourceKey[RegionSetupImportKeys.IndustryProfilesPrefix.Length..];
        var separatorIndex = afterPrefix.IndexOf(RegionSetupImportKeys.SegmentSeparator);
        if (separatorIndex <= 0 || separatorIndex == afterPrefix.Length - 1)
        {
            return null;
        }

        return afterPrefix[(separatorIndex + 1)..];
    }
}
