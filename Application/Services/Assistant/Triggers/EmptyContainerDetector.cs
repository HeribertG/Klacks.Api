// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Scans for active container shifts (ShiftType.IsContainer, ShiftStatus.OriginalShift) that have
/// no ContainerTemplate row at all -- a slot-definition gap, distinct from unstaffed_shift (missing
/// employees on slots that already exist). The anti-join against ContainerTemplate first materializes
/// the set of container ids that already have a template, then filters shifts against that set --
/// two set-based round-trips total, never one query per shift, mirroring the pattern
/// ContainerAvailableTasksService uses for the same kind of exclusion. The groups of the surviving
/// containers are then read in ONE batched lookup (never one query per container), because that group
/// set is what narrows the notification to the planners who may see the container. Emission is capped at
/// MaxFindingsPerTick events per tick (this scan has no time window, unlike UnstaffedShift7dDetector).
/// Ordered by FromDate (oldest gap first), Id as tiebreaker, before the cap applies -- without an
/// explicit order the cap would pick from physical storage order, which is not even stable between ticks.
///
/// That order alone starves NEW findings, which is why a second, smaller slice is added to it. The
/// oldest-first sort degenerates whenever candidates share a FromDate - the normal shape of bulk-created
/// containers: the sort key is then constant, the selection collapses onto the random-GUID tiebreaker, and
/// the cap picks an arbitrary fixed 50 forever. Measured in the reference installation on 2026-08-26: 260
/// candidates, every one of them FromDate 2025-01-01. A container created today sorts behind all of them
/// and would never be reported - and unreported here means invisible everywhere, because the planner
/// notification, ListOpenFindingsSkill, the LLM context, the digest and the action dispatcher all read the
/// LEDGER, which only ever learns what this scan emitted.
///
/// RecentlyCreatedSlots further rows therefore carry candidates the ledger has not opened a row for yet.
/// They are added ON TOP of the cap rather than carved out of it, so the oldest-first selection keeps
/// every one of its slots and no row that was being reported stops being reported - which matters because
/// a row that stops being re-observed also stops having its payload refreshed. The slice stays empty
/// unless the cap actually bit, so it is a no-op for any installation whose findings all fit.
///
/// EARLIER VERSION, KEPT AS A RECORD: this slice used to require "CreateTime strictly greater than every
/// already-selected row" instead. That degenerates the same way the FromDate order does whenever
/// candidates share a CreateTime - the normal shape of bulk-created containers: the floor becomes a value
/// every candidate ties, "strictly greater" is never true, and the second slice returns nothing. Measured
/// in the reference installation on 2026-08-28: 260 candidates, 240 sharing one CreateTime to the
/// microsecond; 50 ever reached the ledger, 210 never did, across 14 real ticks. Excluding by ledger
/// membership instead of by a CreateTime floor has no such degenerate case: a row that was never open
/// stays eligible regardless of what any other row's CreateTime is, and once a tick reports it, it drops
/// out of the pool on its own by becoming open - which is also what makes repeated ticks converge instead
/// of reporting the same RecentlyCreatedSlots rows forever.
///
/// What this does NOT solve: a candidate whose ledger row keeps failing remediation and never reaches a
/// terminal status stays open, and open means excluded here - it will not be picked as a "not yet open"
/// row again, but it is also never dropped from the oldest-first stream once that stream reaches it on
/// FromDate order. Bounded and visible, where the old behaviour was unbounded and silent.
/// </summary>
/// <param name="shiftRepository">Read-only access to container shift candidates.</param>
/// <param name="containerTemplateRepository">Read-only access to the set of container ids that already have a template.</param>
/// <param name="groupScopeReader">Batched shift-to-groups lookup for audience scoping.</param>
/// <param name="agentConditionRepository">Source of the ledger rows still open for this kind, so the second slice can exclude them.</param>
/// <param name="timeProvider">Clock forwarded into each emitted event so its period-active severity check is testable.</param>
/// <param name="logger">Structured log per tick.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Domain.Services.Assistant;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public class EmptyContainerDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    public const int MaxFindingsPerTick = 50;

    /// <summary>
    /// Rows reported IN ADDITION to the cap, carrying the most recently created candidates. Not a share of
    /// MaxFindingsPerTick: taking slots away from the oldest-first selection would stop rows that are being
    /// reported today from being reported, which also stops their payload being refreshed.
    /// </summary>
    public const int RecentlyCreatedSlots = 15;

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
    private readonly TimeProvider _timeProvider;
    private readonly ILogger<EmptyContainerDetector> _logger;

    public EmptyContainerDetector(
        IShiftRepository shiftRepository,
        IContainerTemplateRepository containerTemplateRepository,
        IShiftGroupScopeReader groupScopeReader,
        IAgentConditionRepository agentConditionRepository,
        TimeProvider timeProvider,
        ILogger<EmptyContainerDetector> logger)
    {
        _shiftRepository = shiftRepository;
        _containerTemplateRepository = containerTemplateRepository;
        _groupScopeReader = groupScopeReader;
        _agentConditionRepository = agentConditionRepository;
        _timeProvider = timeProvider;
        _logger = logger;
    }

    public string Kind => AgentTriggerKinds.EmptyContainer;

    public async Task<IReadOnlyList<IAgentTriggerEvent>> DetectAsync(CancellationToken cancellationToken = default)
    {
        var containerIdsWithTemplate = await LoadContainerIdsWithTemplateAsync(cancellationToken);
        var candidates = BuildCandidateQuery(containerIdsWithTemplate);

        var emptyContainers = await candidates
            .OrderBy(s => s.FromDate)
            .ThenBy(s => s.Id)
            .Take(MaxFindingsPerTick)
            .ToListAsync(cancellationToken);

        if (emptyContainers.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

        emptyContainers.AddRange(
            await NotYetOpenInLedgerAsync(candidates, emptyContainers, cancellationToken));

        var groupsByShift = await _groupScopeReader.GetGroupIdsByShiftIdsAsync(
            emptyContainers.Select(container => container.Id).ToList(), cancellationToken);

        var events = emptyContainers
            .Select(container => (IAgentTriggerEvent)new EmptyContainerTriggerEvent(
                container.Id,
                string.IsNullOrWhiteSpace(container.Name) ? container.Abbreviation : container.Name,
                container.FromDate,
                container.UntilDate,
                ShiftGroupScope.For(groupsByShift, container.Id),
                ScheduleSnapshotOf(container),
                EmptyContainerTriggerEvent.ComputeIsPeriodActive(container.FromDate, container.UntilDate, _timeProvider)))
            .ToList();

        _logger.LogInformation(
            "EmptyContainer scan: {Events} empty container(s) with no template found",
            events.Count);

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
    /// Up to RecentlyCreatedSlots candidates the ledger has no open row for yet, excluding the rows the
    /// oldest-first selection already holds, or nothing at all when the cap did not bite.
    ///
    /// Excluding by ledger membership rather than by "newer than every selected row" is what makes this
    /// immune to CreateTime ties: a row that was never opened stays eligible no matter what any other
    /// row's CreateTime is. It also makes the two sets provably disjoint (both exclusions apply before the
    /// query runs), so the caller appends without de-duplicating, and self-limiting the same way the old
    /// CreateTime floor was: once every candidate has an open ledger row, this returns nothing.
    /// </summary>
    private async Task<List<Shift>> NotYetOpenInLedgerAsync(
        IQueryable<Shift> candidates,
        List<Shift> selected,
        CancellationToken cancellationToken)
    {
        if (selected.Count < MaxFindingsPerTick)
        {
            return [];
        }

        var openEntityIds = (await _agentConditionRepository.GetOpenByKindAsync(Kind, cancellationToken))
            .Where(condition => condition.EntityId.HasValue)
            .Select(condition => condition.EntityId!.Value)
            .ToHashSet();
        var selectedIds = selected.Select(container => container.Id).ToHashSet();

        return await candidates
            .Where(container => !selectedIds.Contains(container.Id) && !openEntityIds.Contains(container.Id))
            .OrderByDescending(container => container.CreateTime)
            .ThenBy(container => container.FromDate)
            .ThenBy(container => container.Id)
            .Take(RecentlyCreatedSlots)
            .ToListAsync(cancellationToken);
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
