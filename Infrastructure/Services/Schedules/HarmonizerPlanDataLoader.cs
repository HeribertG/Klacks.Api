// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The database reads of <see cref="HarmonizerContextBuilder"/> that need nothing but the context: the works, breaks, schedule
/// command keywords and clients of the run's agents in a period (scenario-isolated by the AnalyseToken, exactly one token or
/// the real plan), the works a scenario leaves outside itself, and the individual periods of the contracts in play.
/// </summary>

using Klacks.Api.Application.Services.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Models.Staffs;
using Klacks.Api.Infrastructure.Persistence;
using Klacks.ScheduleOptimizer.Models;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.Schedules;

internal static class HarmonizerPlanDataLoader
{
    internal static async Task<List<Work>> LoadWorksAsync(
        DataBaseContext context,
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
    {
        return await context.Work
            .AsNoTracking()
            .Where(w => agentIds.Contains(w.ClientId)
                        && w.CurrentDate >= from
                        && w.CurrentDate <= until
                        && (w.AnalyseToken == analyseToken || (w.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);
    }

    /// <summary>
    /// A scenario carries only the works on the shifts it cloned; the agents' real works elsewhere (another group) stay in
    /// the plan and occupy their days. The real plan already holds every work, so a run without token reads none.
    /// </summary>
    internal static async Task<List<Work>> LoadOutsideScenarioWorksAsync(
        DataBaseContext context,
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
        => analyseToken is { } token
            ? await OutsideScenarioWorkQuery.LoadAsync(context, token, agentIds, from, until, ct)
            : [];

    internal static async Task<Dictionary<Guid, Client>> LoadClientsAsync(DataBaseContext context, List<Guid> agentIds, CancellationToken ct)
    {
        var clients = await context.Client
            .AsNoTracking()
            .Where(c => agentIds.Contains(c.Id))
            .ToListAsync(ct);
        return clients.ToDictionary(c => c.Id);
    }

    internal static async Task<HarmonizerKeywordDays> LoadKeywordDaysAsync(
        DataBaseContext context,
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        IReadOnlyDictionary<string, ScheduleCommandKeyword> keywordMap,
        CancellationToken ct)
    {
        var rawCommands = await context.ScheduleCommands
            .AsNoTracking()
            .Where(c => agentIds.Contains(c.ClientId)
                        && c.CurrentDate >= from
                        && c.CurrentDate <= until
                        && (c.AnalyseToken == analyseToken || (c.AnalyseToken == null && analyseToken == null)))
            .Select(c => new { c.ClientId, c.CurrentDate, c.CommandKeyword })
            .ToListAsync(ct);

        var recognized = new List<(Guid AgentId, DateOnly Date, ScheduleCommandKeyword Keyword)>(rawCommands.Count);
        foreach (var cmd in rawCommands)
        {
            if (ScheduleCommandKeywordMapper.TryMap(cmd.CommandKeyword, keywordMap, out var keyword))
            {
                recognized.Add((cmd.ClientId, cmd.CurrentDate, keyword));
            }
        }

        return HarmonizerKeywordDayResolver.Resolve(recognized);
    }

    internal static async Task<List<Break>> LoadBreaksAsync(
        DataBaseContext context,
        List<Guid> agentIds,
        DateOnly from,
        DateOnly until,
        Guid? analyseToken,
        CancellationToken ct)
    {
        return await context.Break
            .AsNoTracking()
            .Where(b => agentIds.Contains(b.ClientId)
                        && b.CurrentDate >= from
                        && b.CurrentDate <= until
                        && (b.AnalyseToken == analyseToken || (b.AnalyseToken == null && analyseToken == null)))
            .ToListAsync(ct);
    }

    internal static async Task<IReadOnlyDictionary<Guid, IReadOnlyCollection<Period>>> LoadIndividualPeriodsAsync(
        DataBaseContext context,
        IReadOnlyDictionary<DateOnly, Dictionary<Guid, EffectiveContractData>> contractDataByDate,
        CancellationToken ct)
    {
        var individualContractIds = contractDataByDate.Values
            .SelectMany(perDay => perDay.Values)
            .Where(d => (d.GuaranteedHoursBasisInterval ?? d.PaymentInterval) == (int)PaymentInterval.Individual && d.ContractId.HasValue)
            .Select(d => d.ContractId!.Value)
            .Distinct()
            .ToList();

        if (individualContractIds.Count == 0)
        {
            return new Dictionary<Guid, IReadOnlyCollection<Period>>();
        }

        var contracts = await context.Contract
            .AsNoTracking()
            .Where(c => individualContractIds.Contains(c.Id) && c.IndividualPeriod != null)
            .Include(c => c.IndividualPeriod!)
                .ThenInclude(i => i.Periods)
            .ToListAsync(ct);

        return contracts.ToDictionary(
            c => c.Id,
            c => (IReadOnlyCollection<Period>)c.IndividualPeriod!.Periods.ToList());
    }
}
