// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Replacement request book hooks of the scenario lifecycle. On accept, every row of the scenario that is not
/// yet applied, still Proposed/Requested/Accepted and whose replacement WorkChange is actually promoted gets AppliedAtUtc and, where still empty,
/// the WorkChange id; a row without a promoted change (an alternative that was only phoned, or a proposal the
/// planner removed) stays unapplied, a Declined/NotReached row whose person was applied anyway is logged instead of
/// stamped (the recorded answer contradicts the plan and must not be overwritten), and a second run changes nothing. On reject/delete only the engine's
/// untouched Proposed rows are soft-deleted; recorded answers are facts and stay. Both only stage changes, so
/// they commit atomically with the accept or reject itself.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public partial class AnalyseScenarioService
{
    private const string DeclinedButAppliedMessage =
        "Replacement request {RequestId} has outcome {Outcome} but its replacement was applied with scenario {Token}; not stamped as applied";

    private static readonly ReplacementRequestOutcome[] StampableOutcomes =
    [
        ReplacementRequestOutcome.Proposed,
        ReplacementRequestOutcome.Requested,
        ReplacementRequestOutcome.Accepted,
    ];

    private async Task StageReplacementRequestsAppliedAsync(
        Guid token,
        IReadOnlyCollection<Guid> promotedWorkIds,
        IReadOnlyDictionary<Guid, Guid> cloneToSourceShift,
        DateTime nowUtc,
        CancellationToken ct)
    {
        if (promotedWorkIds.Count == 0)
        {
            return;
        }

        var rows = await _context.ReplacementRequests
            .Where(r => r.AnalyseToken == token && r.AppliedAtUtc == null)
            .ToListAsync(ct);
        if (rows.Count == 0)
        {
            return;
        }

        var workIds = promotedWorkIds.ToList();
        var replacementTypes = ReplacementWorkChangeTypes.All.ToList();
        var promotedChanges = await (
                from wc in _context.WorkChange.IgnoreQueryFilters()
                join w in _context.Work.IgnoreQueryFilters() on wc.WorkId equals w.Id
                where wc.AnalyseToken == token
                    && !wc.IsDeleted
                    && wc.ReplaceClientId != null
                    && replacementTypes.Contains(wc.Type)
                    && workIds.Contains(w.Id)
                select new { wc.Id, CandidateId = wc.ReplaceClientId!.Value, w.ShiftId, w.CurrentDate })
            .ToListAsync(ct);

        var changeBySlot = promotedChanges
            .GroupBy(c => (c.CandidateId, ShiftId: cloneToSourceShift.TryGetValue(c.ShiftId, out var source) ? source : c.ShiftId, c.CurrentDate))
            .ToDictionary(g => g.Key, g => g.First().Id);

        foreach (var row in rows)
        {
            if (!changeBySlot.TryGetValue((row.CandidateClientId, row.ShiftId, row.Date), out var workChangeId))
            {
                continue;
            }

            if (!StampableOutcomes.Contains(row.Outcome))
            {
                _logger.LogWarning(DeclinedButAppliedMessage, row.Id, row.Outcome, token);
                continue;
            }

            row.AppliedAtUtc = nowUtc;
            row.WorkChangeId ??= workChangeId;
        }
    }

    private async Task StageProposedReplacementRequestsDeletedAsync(Guid token, DateTime nowUtc, CancellationToken ct)
    {
        var proposed = await _context.ReplacementRequests
            .Where(r => r.AnalyseToken == token && r.Outcome == ReplacementRequestOutcome.Proposed)
            .ToListAsync(ct);

        foreach (var row in proposed)
        {
            row.IsDeleted = true;
            row.DeletedTime = nowUtc;
        }
    }
}
