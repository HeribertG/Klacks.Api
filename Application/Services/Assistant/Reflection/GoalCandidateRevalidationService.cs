// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Expires open goal candidates whose observation has stopped occurring. A candidate is drawn from
/// the trigger dispatch history by TriggerHistoryGoalSignalSource and then never looked at again:
/// GetGoalCandidatesQueryHandler filters on status only, so a proposal keeps being offered until a
/// human decides it, however long ago the condition behind it disappeared. Observed in the reference
/// installation on 2026-09-07: three candidates from 2026-08-30/31 were still in the inbox although
/// none of their detectors had dispatched anything for a week.
///
/// The check reuses the source the candidates were built from rather than re-running twenty
/// detectors: a candidate stays valid while a dispatch row for its (user, goal type) exists inside
/// the same LookbackDays window TriggerHistoryGoalSignalSource reflects over. When the detector stops
/// firing, its rows age out of that window and the candidate expires with them — which also means a
/// candidate survives roughly LookbackDays after the underlying condition is fixed. That delay is
/// intentional: a detector that reports every few days must not lose its candidate between two ticks.
///
/// A candidate younger than GraceHours is never expired, no matter what the history says. The
/// reflection cycle and the trigger scan run on independent schedules, and without the grace a
/// candidate written moments before a dispatch row is trimmed would be expired by its own creator.
///
/// When the dispatch query hits its row cap the whole pass is abandoned rather than run on a
/// truncated window — a truncated history looks exactly like a history in which nothing fired, and
/// acting on that would expire every open candidate in the installation at once.
/// </summary>
/// <param name="dispatchRepository">Read-only access to the trigger dispatch history.</param>
/// <param name="goalCandidateRepository">Loads open candidates and persists the expiry.</param>
/// <param name="timeProvider">Clock the window and the grace period are measured from.</param>
/// <param name="logger">Structured log of how many candidates were expired per cycle.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Logging;

namespace Klacks.Api.Application.Services.Assistant.Reflection;

public class GoalCandidateRevalidationService : IGoalCandidateRevalidationService
{
    private const int LookbackDays = 7;
    private const int GraceHours = 24;
    private const int MaxDispatchRows = 2000;
    private const int MaxCandidatesPerCycle = 500;

    private readonly IProactiveTriggerDispatchRepository _dispatchRepository;
    private readonly IGoalCandidateRepository _goalCandidateRepository;
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<GoalCandidateRevalidationService> _logger;

    public GoalCandidateRevalidationService(
        IProactiveTriggerDispatchRepository dispatchRepository,
        IGoalCandidateRepository goalCandidateRepository,
        TimeProvider timeProvider,
        ILogger<GoalCandidateRevalidationService> logger)
    {
        _dispatchRepository = dispatchRepository;
        _goalCandidateRepository = goalCandidateRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public async Task<int> RunRevalidationCycleAsync(CancellationToken cancellationToken = default)
    {
        var candidates = await _goalCandidateRepository.GetOpenAsync(MaxCandidatesPerCycle, cancellationToken);
        if (candidates.Count == 0)
        {
            return 0;
        }

        var nowUtc = _timeProvider.GetUtcNow().UtcDateTime;
        var rows = await _dispatchRepository.GetSinceAsync(
            nowUtc.AddDays(-LookbackDays), MaxDispatchRows, cancellationToken);

        if (rows.Count == MaxDispatchRows)
        {
            _logger.LogWarning(
                "Goal candidate revalidation skipped — the dispatch history hit the {MaxDispatchRows}-row cap, " +
                "and a truncated window is indistinguishable from an empty one",
                MaxDispatchRows);

            return 0;
        }

        var stillObserved = rows
            .Where(row => !string.IsNullOrWhiteSpace(row.UserId))
            .Select(row => BuildKey(row.UserId, row.TriggerKind))
            .ToHashSet(StringComparer.Ordinal);

        var graceCutoffUtc = nowUtc.AddHours(-GraceHours);
        var expired = 0;

        foreach (var candidate in candidates)
        {
            if (candidate.CreateTime is DateTime createdUtc && createdUtc > graceCutoffUtc)
            {
                continue;
            }

            if (stillObserved.Contains(BuildKey(candidate.UserId, candidate.GoalType)))
            {
                continue;
            }

            candidate.Status = GoalCandidateStatus.Expired;
            candidate.DecidedUtc = nowUtc;
            await _goalCandidateRepository.UpdateAsync(candidate, cancellationToken);
            expired++;

            _logger.LogInformation(
                "Goal candidate {CandidateId} of type {GoalType} expired for user {UserId} — no observation in the last {LookbackDays} day(s)",
                candidate.Id, candidate.GoalType, candidate.UserId.ForLog(), LookbackDays);
        }

        _logger.LogInformation(
            "Goal candidate revalidation complete — {Expired} of {Total} open candidate(s) expired",
            expired, candidates.Count);

        return expired;
    }

    private static string BuildKey(string? userId, string? goalType) =>
        (userId ?? string.Empty) + "|" + (goalType ?? string.Empty);
}
