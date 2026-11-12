// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Accept check for every scenario, before anything is staged: an accept must never delete a real absence the scenario
/// does not carry. It selects the real breaks with the same selection logic as the delete step
/// (ScenarioRealPlanReplacer.SelectDeletionAsync: accept scope, day-lock plan, live rows only), evaluated here and again by
/// the delete step, keeps the absences among them (no pause of a work) and looks for a twin
/// of each in the scenario by content - same person, same day, same absence type - never by time stamps. Any absence
/// without a twin refuses the accept with ScenarioDropsRealAbsenceException, which no override lifts.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="realPlanReplacer">Selects the real rows the accept deletes</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioRealAbsenceGuard
{
    private const int MaxReportedDroppedAbsences = 10;

    private readonly DataBaseContext _context;
    private readonly ScenarioRealPlanReplacer _realPlanReplacer;

    public ScenarioRealAbsenceGuard(DataBaseContext context, ScenarioRealPlanReplacer realPlanReplacer)
    {
        _context = context;
        _realPlanReplacer = realPlanReplacer;
    }

    public async Task EnsureNoRealAbsenceDroppedAsync(AnalyseScenario scenario, CancellationToken ct)
    {
        var deletion = await _realPlanReplacer.SelectDeletionAsync(
            scenario.GroupId, scenario.Token, scenario.FromDate, scenario.UntilDate, ct);

        var realAbsences = deletion.Breaks.Where(b => b.ParentWorkId == null).ToList();
        if (realAbsences.Count == 0)
        {
            return;
        }

        var twins = await LoadScenarioAbsenceKeysAsync(scenario, realAbsences.Select(b => b.ClientId).Distinct().ToList(), ct);
        var dropped = realAbsences
            .Where(b => !twins.Contains((b.ClientId, b.CurrentDate, b.AbsenceId)))
            .ToList();
        if (dropped.Count == 0)
        {
            return;
        }

        throw new ScenarioDropsRealAbsenceException(dropped.Count, await DescribeAsync(dropped, ct));
    }

    private async Task<HashSet<(Guid ClientId, DateOnly Date, Guid AbsenceId)>> LoadScenarioAbsenceKeysAsync(
        AnalyseScenario scenario, List<Guid> clientIds, CancellationToken ct)
    {
        var token = scenario.Token;
        var fromDate = scenario.FromDate;
        var untilDate = scenario.UntilDate;

        var keys = await _context.Break.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(b => b.AnalyseToken == token && !b.IsDeleted && b.ParentWorkId == null
                && clientIds.Contains(b.ClientId)
                && b.CurrentDate >= fromDate && b.CurrentDate <= untilDate)
            .Select(b => new { b.ClientId, b.CurrentDate, b.AbsenceId })
            .ToListAsync(ct);

        return keys.Select(k => (k.ClientId, k.CurrentDate, k.AbsenceId)).ToHashSet();
    }

    private async Task<IReadOnlyList<DroppedRealAbsence>> DescribeAsync(List<Break> dropped, CancellationToken ct)
    {
        var clientIds = dropped.Select(b => b.ClientId).Distinct().ToList();
        var names = await _context.Client.IgnoreQueryFilters()
            .AsNoTracking()
            .Where(c => clientIds.Contains(c.Id))
            .Select(c => new { c.Id, c.FirstName, c.Name })
            .ToDictionaryAsync(
                c => c.Id,
                c => string.IsNullOrWhiteSpace(c.FirstName) ? c.Name : $"{c.FirstName} {c.Name}",
                ct);

        return dropped
            .Select(b => new DroppedRealAbsence(b.ClientId, names.GetValueOrDefault(b.ClientId) ?? string.Empty, b.CurrentDate, b.AbsenceId))
            .OrderBy(a => a.Date)
            .ThenBy(a => a.ClientName, StringComparer.Ordinal)
            .ThenBy(a => a.ClientId)
            .Take(MaxReportedDroppedAbsences)
            .ToList();
    }
}
