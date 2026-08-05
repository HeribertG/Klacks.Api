// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Persistence for grounding shadow-run telemetry. Daily counters are incremented with an
/// atomic ON CONFLICT upsert because findings arrive from concurrent fire-and-forget tasks;
/// a read-modify-write would lose updates.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.Grounding;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Repositories.Assistant;

public class AnswerGroundingRepository : IAnswerGroundingRepository
{
    private readonly DataBaseContext _context;

    public AnswerGroundingRepository(DataBaseContext context)
    {
        _context = context;
    }

    public async Task AddFindingAsync(AnswerGroundingFinding finding, CancellationToken cancellationToken = default)
    {
        _context.AnswerGroundingFindings.Add(finding);
        await _context.SaveChangesAsync(cancellationToken);
    }

    public async Task IncrementDailyAsync(AnswerGroundingDailyCounter delta, CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow;
        await _context.Database.ExecuteSqlInterpolatedAsync($@"
INSERT INTO answer_grounding_daily_counters
    (id, day, agent_id, evaluator_version,
     turns_evaluated, turns_clean, turns_with_findings, turns_skipped, turns_no_verdict,
     claims_extracted, claims_uncovered,
     create_time, update_time, is_deleted, current_user_created, current_user_deleted, current_user_updated)
VALUES
    ({Guid.NewGuid()}, {delta.Day}, {delta.AgentId}, {delta.EvaluatorVersion},
     {delta.TurnsEvaluated}, {delta.TurnsClean}, {delta.TurnsWithFindings}, {delta.TurnsSkipped}, {delta.TurnsNoVerdict},
     {delta.ClaimsExtracted}, {delta.ClaimsUncovered},
     {now}, {now}, {false}, {"system"}, {string.Empty}, {string.Empty})
ON CONFLICT (day, agent_id, evaluator_version) WHERE is_deleted = false DO UPDATE SET
    turns_evaluated = answer_grounding_daily_counters.turns_evaluated + EXCLUDED.turns_evaluated,
    turns_clean = answer_grounding_daily_counters.turns_clean + EXCLUDED.turns_clean,
    turns_with_findings = answer_grounding_daily_counters.turns_with_findings + EXCLUDED.turns_with_findings,
    turns_skipped = answer_grounding_daily_counters.turns_skipped + EXCLUDED.turns_skipped,
    turns_no_verdict = answer_grounding_daily_counters.turns_no_verdict + EXCLUDED.turns_no_verdict,
    claims_extracted = answer_grounding_daily_counters.claims_extracted + EXCLUDED.claims_extracted,
    claims_uncovered = answer_grounding_daily_counters.claims_uncovered + EXCLUDED.claims_uncovered,
    update_time = EXCLUDED.update_time", cancellationToken);
    }
}
