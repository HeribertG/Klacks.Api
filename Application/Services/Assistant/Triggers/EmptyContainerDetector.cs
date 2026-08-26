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
/// RecentlyCreatedSlots further rows therefore carry the most recently CREATED candidates. They are added
/// ON TOP of the cap rather than carved out of it, so the oldest-first selection keeps every one of its
/// slots and no row that was being reported stops being reported - which matters because a row that stops
/// being re-observed also stops having its payload refreshed. The slice stays empty unless the cap
/// actually bit AND something is genuinely newer than everything already selected, so it is a no-op for
/// any installation whose findings all fit. It orders by CreateTime rather than FromDate precisely so a
/// container created today is caught even when its period is backdated.
///
/// What this does NOT solve: if the newest RecentlyCreatedSlots candidates are themselves never
/// remediated, the one after them starves again. Bounded and visible, where the old behaviour was
/// unbounded and silent.
/// </summary>
/// <param name="shiftRepository">Read-only access to container shift candidates.</param>
/// <param name="containerTemplateRepository">Read-only access to the set of container ids that already have a template.</param>
/// <param name="groupScopeReader">Batched shift-to-groups lookup for audience scoping.</param>
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
    private readonly ILogger<EmptyContainerDetector> _logger;

    public EmptyContainerDetector(
        IShiftRepository shiftRepository,
        IContainerTemplateRepository containerTemplateRepository,
        IShiftGroupScopeReader groupScopeReader,
        ILogger<EmptyContainerDetector> logger)
    {
        _shiftRepository = shiftRepository;
        _containerTemplateRepository = containerTemplateRepository;
        _groupScopeReader = groupScopeReader;
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
            await NewerThanEverySelectedAsync(candidates, emptyContainers, cancellationToken));

        var groupsByShift = await _groupScopeReader.GetGroupIdsByShiftIdsAsync(
            emptyContainers.Select(container => container.Id).ToList(), cancellationToken);

        var events = emptyContainers
            .Select(container => (IAgentTriggerEvent)new EmptyContainerTriggerEvent(
                container.Id,
                string.IsNullOrWhiteSpace(container.Name) ? container.Abbreviation : container.Name,
                container.FromDate,
                container.UntilDate,
                ShiftGroupScope.For(groupsByShift, container.Id),
                ScheduleSnapshotOf(container)))
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
    /// The most recently created candidates that are newer than EVERY row the oldest-first selection
    /// already holds, or nothing at all when the cap did not bite.
    ///
    /// "Strictly newer than every selected row" is what makes the two sets provably disjoint, so the
    /// caller appends without de-duplicating. It also makes the slice self-limiting: when all candidates
    /// fit under the cap there is nothing newer left to find, and when they do not, only containers created
    /// after the reported ones qualify - which is exactly the population the FromDate order cannot reach.
    /// A selection carrying no CreateTime at all yields nothing, because there is then no floor to compare
    /// against; those rows are already covered by the oldest-first stream.
    /// </summary>
    private static async Task<List<Shift>> NewerThanEverySelectedAsync(
        IQueryable<Shift> candidates,
        List<Shift> selected,
        CancellationToken cancellationToken)
    {
        if (selected.Count < MaxFindingsPerTick)
        {
            return [];
        }

        var newestSelected = selected.Max(container => container.CreateTime);
        if (newestSelected is null)
        {
            return [];
        }

        return await candidates
            .Where(container => container.CreateTime > newestSelected)
            .OrderByDescending(container => container.CreateTime)
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
