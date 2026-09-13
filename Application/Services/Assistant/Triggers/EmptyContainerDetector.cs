// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans for active container shifts (ShiftType.IsContainer, ShiftStatus.OriginalShift) that have
/// no ContainerTemplate row at all -- a slot-definition gap, distinct from unstaffed_shift (missing
/// employees on slots that already exist). The anti-join against ContainerTemplate first materializes
/// the set of container ids that already have a template, then filters shifts against that set,
/// set-based rather than one query per shift, mirroring the pattern ContainerAvailableTasksService uses
/// for the same kind of exclusion. The groups of the reported containers are then read in ONE batched
/// lookup (never one query per container), because that group set is what narrows the notification to
/// the planners who may see the container.
///
/// Emission is capped at MaxFindingsPerTick events per tick (this scan has no time window, unlike
/// UnstaffedShift7dDetector), and which candidates fill that cap ROTATES: AgentConditionRotationPolicy
/// puts the containers the ledger has never opened a row for first, and behind them the open rows least
/// recently observed. A plain oldest-FromDate-first cap cannot do this - the sort key is constant across
/// bulk-created containers (measured in the reference installation: 260 candidates, every one of them
/// FromDate 2025-01-01), so the selection collapses onto the random-GUID tiebreaker and picks the same
/// fixed 50 forever. Rotation matters because a Reported row means "handed to the notification
/// pipeline", NOT "delivered": AgentTriggerService drops the message when the recipient has hit the
/// per-user daily cap, and the row still becomes Reported. A recipient who was throttled must therefore
/// be offered every row again on a later tick, which is precisely what least-recently-observed ordering
/// guarantees - LastSeenAtUtc is advanced on every row this tick reports, so the whole backlog cycles in
/// ceil(candidates / MaxFindingsPerTick) ticks.
///
/// What rotation costs: a row that is not re-observed in a tick also has no payload refresh in that
/// tick, so a reported container's PayloadJson can be up to one full cycle old. Bounded staleness is the
/// deliberate trade for the previous behaviour, where the rows behind the fixed cap were never reported
/// at all.
/// </summary>
/// <param name="shiftRepository">Read-only access to container shift candidates.</param>
/// <param name="containerTemplateRepository">Read-only access to the set of container ids that already have a template.</param>
/// <param name="groupScopeReader">Batched shift-to-groups lookup for audience scoping.</param>
/// <param name="agentConditionRepository">Source of the ledger rows still open for this kind, whose LastSeenAtUtc drives the rotation.</param>
/// <param name="companyClock">Resolves "today" as the company's own local day for the period-active severity check.</param>
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

public class EmptyContainerDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    public const int MaxFindingsPerTick = 50;

    /// <summary>
    /// ISO weekday number (1 = Monday .. 7 = Sunday) per weekday flag of a Shift, in ascending order, so
    /// the snapshot below is a projection rather than seven branches that can each be mistyped.
    /// </summary>
    private static readonly IReadOnlyList<(Func<Shift, bool> IsSet, int IsoWeekday)> WeekdayFlags =
    [
        (shift => shift.IsMonday, 1),
        (shift => shift.IsTuesday, 2),
        (shift => shift.IsWednesday, 3),
        (shift => shift.IsThursday, 4),
        (shift => shift.IsFriday, 5),
        (shift => shift.IsSaturday, 6),
        (shift => shift.IsSunday, 7)
    ];

    private readonly IShiftRepository _shiftRepository;
    private readonly IContainerTemplateRepository _containerTemplateRepository;
    private readonly IShiftGroupScopeReader _groupScopeReader;
    private readonly IAgentConditionRepository _agentConditionRepository;
    private readonly ICompanyClock _companyClock;
    private readonly ILogger<EmptyContainerDetector> _logger;

    public EmptyContainerDetector(
        IShiftRepository shiftRepository,
        IContainerTemplateRepository containerTemplateRepository,
        IShiftGroupScopeReader groupScopeReader,
        IAgentConditionRepository agentConditionRepository,
        ICompanyClock companyClock,
        ILogger<EmptyContainerDetector> logger)
    {
        _shiftRepository = shiftRepository;
        _containerTemplateRepository = containerTemplateRepository;
        _groupScopeReader = groupScopeReader;
        _agentConditionRepository = agentConditionRepository;
        _companyClock = companyClock;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.EmptyContainer;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var containerIdsWithTemplate = await LoadContainerIdsWithTemplateAsync(cancellationToken);

        var candidates = await BuildCandidateQuery(containerIdsWithTemplate)
            .Select(s => new ContainerCandidate(s.Id, s.FromDate))
            .ToListAsync(cancellationToken);

        if (candidates.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var lastSeenByContainerId = await LoadLastSeenByContainerIdAsync(cancellationToken);

        var selectedIds = AgentConditionRotationPolicy
            .Select(
                candidates,
                candidate => candidate.Id,
                lastSeenByContainerId,
                CompareByFromDateThenId,
                MaxFindingsPerTick)
            .Select(candidate => candidate.Id)
            .ToList();

        var emptyContainers = await LoadInRotationOrderAsync(
            containerIdsWithTemplate, selectedIds, cancellationToken);

        if (emptyContainers.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        var groupsByShift = await _groupScopeReader.GetGroupIdsByShiftIdsAsync(
            emptyContainers.Select(container => container.Id).ToList(), cancellationToken);
        var today = await _companyClock.GetTodayDateAsync(cancellationToken);

        var events = emptyContainers
            .Select(container => (IAgentTriggerEvent)new EmptyContainerTriggerEvent(
                container.Id,
                string.IsNullOrWhiteSpace(container.Name) ? container.Abbreviation : container.Name,
                container.FromDate,
                container.UntilDate,
                ShiftGroupScope.For(groupsByShift, container.Id),
                ScheduleSnapshotOf(container),
                EmptyContainerTriggerEvent.ComputeIsPeriodActive(container.FromDate, container.UntilDate, today)))
            .ToList();

        _logger.LogInformation(
            "EmptyContainer scan: {Events} of {Candidates} empty container(s) with no template reported this tick",
            events.Count,
            candidates.Count);

        return events;
    }

    public async Task<IReadOnlySet<string>> GetActiveFingerprintsAsync(CancellationToken cancellationToken = default)
    {
        var containerIdsWithTemplate = await LoadContainerIdsWithTemplateAsync(cancellationToken);

        var containerIds = await BuildCandidateQuery(containerIdsWithTemplate)
            .Select(s => s.Id)
            .ToListAsync(cancellationToken);

        return containerIds
            .Select(containerId => AgentConditionLedgerPolicy.FingerprintFor(
                Kind,
                EmptyContainerTriggerEvent.DedupKeyFor(containerId)))
            .ToHashSet(StringComparer.Ordinal);
    }

    /// <summary>
    /// Identity and business order of one candidate, kept as a projection so the rotation input costs two
    /// columns per candidate instead of a full Shift row for a backlog that is deliberately uncapped here.
    /// </summary>
    private sealed record ContainerCandidate(Guid Id, DateOnly FromDate);

    private static int CompareByFromDateThenId(ContainerCandidate left, ContainerCandidate right)
    {
        var byFromDate = left.FromDate.CompareTo(right.FromDate);

        return byFromDate != 0 ? byFromDate : left.Id.CompareTo(right.Id);
    }

    /// <summary>
    /// LastSeenAtUtc per container of the ledger rows still OPEN for this kind; rows without an EntityId
    /// cannot be matched to a container and are ignored. Should several open rows ever share one entity,
    /// the OLDEST observation wins, so the container is offered sooner rather than later.
    /// </summary>
    private async Task<IReadOnlyDictionary<Guid, DateTime>> LoadLastSeenByContainerIdAsync(
        CancellationToken cancellationToken) =>
        (await _agentConditionRepository.GetOpenByKindAsync(Kind, cancellationToken))
            .Where(condition => condition.EntityId.HasValue)
            .GroupBy(condition => condition.EntityId!.Value)
            .ToDictionary(
                group => group.Key,
                group => group.Min(condition => condition.LastSeenAtUtc));

    /// <summary>
    /// The full Shift rows behind the selected ids, re-sorted into the order the rotation produced -
    /// GetQuery() carries an OrderBy of its own, so the database order is not the rotation order. Ids that
    /// no longer match the candidate predicates (a template added, or a soft delete, between the two
    /// queries) simply drop out rather than being reported from a stale projection.
    /// </summary>
    private async Task<List<Shift>> LoadInRotationOrderAsync(
        List<Guid> containerIdsWithTemplate,
        List<Guid> selectedIds,
        CancellationToken cancellationToken)
    {
        var containersById = await BuildCandidateQuery(containerIdsWithTemplate)
            .Where(s => selectedIds.Contains(s.Id))
            .ToDictionaryAsync(s => s.Id, cancellationToken);

        return selectedIds
            .Where(containersById.ContainsKey)
            .Select(containerId => containersById[containerId])
            .ToList();
    }

    private async Task<List<Guid>> LoadContainerIdsWithTemplateAsync(CancellationToken cancellationToken) =>
        await _containerTemplateRepository.GetQuery()
            .Where(t => !t.IsDeleted)
            .Select(t => t.ContainerId)
            .Distinct()
            .ToListAsync(cancellationToken);

    /// <summary>
    /// The container's own definition, carried into the payload so the Etappe 5b remediation binder can
    /// stay a pure function over it. Ascending ISO weekdays with Sunday as 7, matching what
    /// create_container_template expects - the 0-for-Sunday spelling is ContainerTemplate's storage
    /// form and is converted inside the skill, not here.
    /// </summary>
    private static ContainerScheduleSnapshot ScheduleSnapshotOf(Shift container)
    {
        var isoWeekdays = WeekdayFlags
            .Where(flag => flag.IsSet(container))
            .Select(flag => flag.IsoWeekday)
            .ToList();

        return new ContainerScheduleSnapshot(
            container.StartShift,
            container.EndShift,
            isoWeekdays,
            container.IsHoliday,
            container.IsWeekdayAndHoliday);
    }

    private IQueryable<Shift> BuildCandidateQuery(List<Guid> containerIdsWithTemplate) =>
        _shiftRepository.GetQuery()
            .Where(s => s.ShiftType == ShiftType.IsContainer)
            .Where(s => s.Status == ShiftStatus.OriginalShift)
            .Where(s => s.AnalyseToken == null && s.ScenarioSourceShiftId == null)
            .Where(s => !s.IsDeleted)
            .Where(s => !containerIdsWithTemplate.Contains(s.Id));
}
