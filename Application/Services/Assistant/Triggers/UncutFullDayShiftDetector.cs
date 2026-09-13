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
/// as stale.
///
/// Candidates are loaded UNCAPPED -- a two-column Id/FromDate projection, not full Shift rows, so the
/// uncapped read stays cheap -- and which MaxFindingsPerTick of them fill one tick's emission ROTATES:
/// AgentConditionRotationPolicy puts the shifts the ledger has never opened a row for first, and behind
/// them the open rows least recently observed, with proximity to today (nearest FromDate first, in
/// either time direction) as the business order and tiebreaker inside both groups. A plain
/// nearest-to-today cap without rotation starves whatever sits behind position MaxFindingsPerTick
/// whenever many candidates cluster at the same proximity to today, the same bug class
/// EmptyContainerDetector had before its own rotation. LastSeenAtUtc is advanced on every row a tick
/// reports, so the whole backlog cycles in ceil(candidates / MaxFindingsPerTick) ticks instead of the
/// tail behind a fixed window never being reported at all.
///
/// What rotation costs here, more visibly than for EmptyContainer: the business order is urgency
/// (proximity to today), not a stable creation date, so a shift reported on one tick can rank BEHIND
/// far-out, never-opened shifts on the very next tick even though it is now one day more urgent than
/// before. A recipient who was throttled on tick 1 is guaranteed to see it again within one cycle, just
/// not necessarily as the most urgent item in that later tick's list.
///
/// The selected ids are re-read from BuildCandidateQuery right before events are built, purely as a
/// freshness re-check -- a cut or soft-delete landing between the two queries drops the shift instead
/// of reporting it from a stale projection. The projection already carries everything
/// UncutFullDayShiftTriggerEvent needs (ShiftId, FromDate, DaysUntil), so this second query adds no
/// column the first one lacked.
/// The groups of the shifts that survive both stages are read in ONE batched lookup (never one query
/// per shift), because that group set is what narrows the notification to the planners who may see the
/// shift.
/// </summary>
/// <param name="shiftRepository">Read-only shift scans via GetQuery().</param>
/// <param name="groupScopeReader">Batched shift-to-groups lookup for audience scoping.</param>
/// <param name="agentConditionRepository">Source of the ledger rows still open for this kind, whose LastSeenAtUtc drives the rotation.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day, not the server's UTC day.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class UncutFullDayShiftDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    public const int MaxFindingsPerTick = 25;

    private readonly IShiftRepository _shiftRepository;
    private readonly IShiftGroupScopeReader _groupScopeReader;
    private readonly IAgentConditionRepository _agentConditionRepository;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<UncutFullDayShiftDetector> _logger;

    public UncutFullDayShiftDetector(
        IShiftRepository shiftRepository,
        IShiftGroupScopeReader groupScopeReader,
        IAgentConditionRepository agentConditionRepository,
        ICompanyClock companyClock,
        ILogger<UncutFullDayShiftDetector> logger)
    {
        _shiftRepository = shiftRepository;
        _groupScopeReader = groupScopeReader;
        _agentConditionRepository = agentConditionRepository;
        _companyClock = companyClock;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.UncutFulldayShift;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var today = await TodayAsync(cancellationToken);

        var candidates = await LoadCandidatesAsync(today, cancellationToken);
        if (candidates.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var lastSeenByShiftId = await LoadLastSeenByShiftIdAsync(cancellationToken);

        var selectedIds = AgentConditionRotationPolicy
            .Select(
                candidates,
                candidate => candidate.Id,
                lastSeenByShiftId,
                CompareByProximityThenFromDateThenId,
                MaxFindingsPerTick)
            .Select(candidate => candidate.Id)
            .ToList();

        var selected = await LoadInRotationOrderAsync(today, selectedIds, cancellationToken);
        if (selected.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var groupsByShift = await _groupScopeReader.GetGroupIdsByShiftIdsAsync(
            selected.Select(candidate => candidate.Id).ToList(), cancellationToken);

        var events = selected
            .Select(candidate => (IAgentTriggerEvent)new UncutFullDayShiftTriggerEvent(
                candidate.Id,
                candidate.FromDate,
                candidate.DaysUntil,
                ShiftGroupScope.For(groupsByShift, candidate.Id)))
            .ToList();

        _logger.LogInformation(
            "UncutFullDayShift scan: {Events} of {Candidates} uncut full-day shift(s) reported this tick",
            events.Count, candidates.Count);

        return events;
    }

    /// <summary>
    /// Skips the MaxFindingsPerTick rotation/cap DetectAsync applies, because a set missing it would
    /// not be complete. Only the Take falls away; the business predicates are the shared
    /// BuildCandidateQuery, already uncapped there too.
    /// </summary>
    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var shiftIds = await BuildCandidateQuery(await TodayAsync(cancellationToken))
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        return shiftIds
            .Select(shiftId => AgentConditionLedgerPolicy.FingerprintFor(
                Kind,
                UncutFullDayShiftTriggerEvent.DedupKeyFor(shiftId)))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Identity and business order of one candidate, kept as a projection so the rotation input costs
    /// two columns per candidate instead of a full Shift row for a backlog that is deliberately
    /// uncapped here.
    /// </summary>
    private sealed record ShiftCandidate(Guid Id, DateOnly FromDate, int DaysUntil);

    private static int CompareByProximityThenFromDateThenId(ShiftCandidate left, ShiftCandidate right)
    {
        var byProximity = Math.Abs(left.DaysUntil).CompareTo(Math.Abs(right.DaysUntil));
        if (byProximity != 0)
        {
            return byProximity;
        }

        var byFromDate = left.FromDate.CompareTo(right.FromDate);

        return byFromDate != 0 ? byFromDate : left.Id.CompareTo(right.Id);
    }

    private async Task<List<ShiftCandidate>> LoadCandidatesAsync(DateOnly today, CancellationToken cancellationToken)
    {
        var projections = await BuildCandidateQuery(today)
            .Select(s => new { s.Id, s.FromDate })
            .ToListAsync(cancellationToken);

        return projections
            .Select(projection => ToCandidate(projection.Id, projection.FromDate, today))
            .ToList();
    }

    /// <summary>
    /// LastSeenAtUtc per shift of the ledger rows still OPEN for this kind; rows without an EntityId
    /// cannot be matched to a shift and are ignored. Should several open rows ever share one entity,
    /// the OLDEST observation wins, so the shift is offered sooner rather than later.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, DateTime>> LoadLastSeenByShiftIdAsync(
        CancellationToken cancellationToken) =>
        (await _agentConditionRepository.GetOpenByKindAsync(Kind, cancellationToken))
            .Where(condition => condition.EntityId.HasValue)
            .GroupBy(condition => condition.EntityId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Min(condition => condition.LastSeenAtUtc));

    /// <summary>
    /// The selected ids re-read from BuildCandidateQuery, re-sorted into the order the rotation
    /// produced - GetQuery() carries an OrderBy of its own, so the database order is not the rotation
    /// order. Ids that no longer match the candidate predicates (a cut or a soft delete landing between
    /// the two queries) simply drop out rather than being reported from a stale projection.
    /// </summary>
    private async Task<List<ShiftCandidate>> LoadInRotationOrderAsync(
        DateOnly today, List<Guid> selectedIds, CancellationToken cancellationToken)
    {
        var projections = await BuildCandidateQuery(today)
            .Where(s => selectedIds.Contains(s.Id))
            .Select(s => new { s.Id, s.FromDate })
            .ToListAsync(cancellationToken);

        var candidatesById = projections
            .ToDictionary(projection => projection.Id, projection => ToCandidate(projection.Id, projection.FromDate, today));

        return selectedIds
            .Where(candidatesById.ContainsKey)
            .Select(id => candidatesById[id])
            .ToList();
    }

    private static ShiftCandidate ToCandidate(Guid id, DateOnly fromDate, DateOnly today) =>
        new(id, fromDate, fromDate.DayNumber - today.DayNumber);

    private Task<DateOnly> TodayAsync(CancellationToken cancellationToken) => _companyClock.GetTodayDateAsync(cancellationToken);

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
