// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans ShiftType.IsTask rows in Status OriginalShift whose StartShift equals EndShift -- the
/// FullDay convention TimeRange.ForWorkingTime assigns to equal bounds, a 24 hour duty rather than
/// a zero-length span -- and that have not yet been cut into day/night pieces (a cut mutates the
/// row's Status to SplitShift, see CutShiftByDateSkill). Container shifts (ShiftType.IsContainer)
/// are excluded on purpose: "cut" is a task-level operation, containers are pure envelopes, so a
/// container with equal StartShift/EndShift is exclusively EmptyContainerDetector's concern, never
/// this detector's. Already-ended shifts (UntilDate before today) are excluded so old, resolved
/// duties never resurface; a shift already under way (FromDate in the past, UntilDate still open)
/// is kept and, being closest to today, ranks as the most urgent finding rather than being dropped
/// as stale. The MaxCandidatesToScan pre-filter is ordered by FromDate (oldest first), Id as
/// tiebreaker, so which candidates make it into the scan window is deterministic and drains oldest-
/// first as duties get cut, rather than depending on physical storage order. Within that window,
/// candidates are then ranked by proximity to today (nearest FromDate first, in either time
/// direction) before capping emission at MaxFindingsPerTick. Note this two-stage ranking is not
/// symmetric: a backlog of long-since-started duties exceeding MaxCandidatesToScan can still crowd a
/// genuinely upcoming one out of the pre-filter itself, because the pre-filter orders one-
/// directionally (oldest FromDate first) while the in-memory ranking that follows it is
/// bidirectional (nearest to today, past or future). Only the second stage delivers the "does not
/// crowd out" guarantee; the first stage merely makes candidate selection deterministic.
/// The groups of the shifts that survive both stages are read in ONE batched lookup (never one query
/// per shift), because that group set is what narrows the notification to the planners who may see the
/// shift.
/// </summary>
/// <param name="shiftRepository">Read-only shift scans via GetQuery().</param>
/// <param name="groupScopeReader">Batched shift-to-groups lookup for audience scoping.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UncutFullDayShiftDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    public const int MaxFindingsPerTick = 25;
    private const int MaxCandidatesToScan = 500;

    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftGroupScopeReader _groupScopeReader;
    private readonly ILogger<UncutFullDayShiftDetector> _logger;

    public UncutFullDayShiftDetector(
        IShiftRepository shiftRepository,
        IShiftGroupScopeReader groupScopeReader,
        ILogger<UncutFullDayShiftDetector> logger)
    {
        _shiftRepository = shiftRepository;
        _groupScopeReader = groupScopeReader;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.UncutFulldayShift;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = Today();

        var candidates = await BuildCandidateQuery(today)
            .OrderBy(s => s.FromDate)
            .ThenBy(s => s.Id)
            .Take(MaxCandidatesToScan)
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var ranked = candidates
            .Select(shift => new
            {
                Shift = shift,
                DaysUntil = shift.FromDate.DayNumber - today.DayNumber
            })
            .OrderBy(x => Math.Abs(x.DaysUntil))
            .Take(MaxFindingsPerTick)
            .ToList();

        var groupsByShift = await _groupScopeReader.GetGroupIdsByShiftIdsAsync(
            ranked.Select(x => x.Shift.Id).ToList(), cancellationToken);

        var events = ranked
            .Select(x => (IAgentTriggerEvent)new UncutFullDayShiftTriggerEvent(
                x.Shift.Id,
                x.Shift.FromDate,
                x.DaysUntil,
                ShiftGroupScope.For(groupsByShift, x.Shift.Id)))
            .ToList();

        _logger.LogInformation(
            "UncutFullDayShift scan: {Total} uncut full-day shift(s) found, {Events} event(s) emitted",
            candidates.Count, events.Count);

        return events;
    }

    /// <summary>
    /// Skips BOTH caps DetectAsync applies - the MaxCandidatesToScan pre-filter in the database and the
    /// MaxFindingsPerTick proximity ranking in memory - because a set missing either one would not be
    /// complete. Only the ordering and the two Take calls fall away; the business predicates are the
    /// shared BuildCandidateQuery.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var shiftIds = await BuildCandidateQuery(Today())
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        return shiftIds
            .Select(shiftId => AgentConditionLedgerPolicy.FingerprintFor(
                Kind,
                UncutFullDayShiftTriggerEvent.DedupKeyFor(shiftId)))
            .ToHashSet(StringComparer.Ordinal);
    }

    private static DateOnly Today() => DateOnly.FromDateTime(DateTime.UtcNow);

    private IQueryable<Shift> BuildCandidateQuery(DateOnly today) =>
        _shiftRepository.GetQuery()
            .Where(s => s.Status == ShiftStatus.OriginalShift
                && s.ShiftType == ShiftType.IsTask
                && s.StartShift == s.EndShift
                && s.AnalyseToken == null
                && s.ScenarioSourceShiftId == null
                && !s.IsDeleted
                && (s.UntilDate == null || s.UntilDate >= today));
}
