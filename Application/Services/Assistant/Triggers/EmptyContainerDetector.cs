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
/// 🔴 KNOWN DEFECT, open as of this commit. The ordering does NOT deliver the oldest-first triage queue
/// it was meant to whenever the candidates share a FromDate, which is the normal shape of bulk-created
/// containers: the sort key is then constant and the selection collapses onto the random-GUID tiebreaker,
/// so the cap picks an ARBITRARY fixed 50 and the rest are never reported. Measured in the reference
/// installation on 2026-08-26: 260 candidates, every one of them FromDate 2025-01-01, and the 50 ledger
/// rows of this kind are exactly this query's top 50. A container created today sorts behind all of them
/// and is never seen while they remain.
///
/// The cost is NOT limited to the autonomous remediation. Every emitted event also goes to the planner
/// notification path unconditionally, so this cap decides what HUMANS are told about as well: 210 of the
/// 260 findings reach nobody, and nothing else can see them either, because ListOpenFindingsSkill, the
/// LLM context renderer, the digest and the action dispatcher all read the LEDGER, which only ever learns
/// about what this scan emitted. ConditionRemediationRegistry does register empty_container ->
/// create_container_template, so once governance for this kind is raised to Execute the selected 50 drain
/// at the default budget of 5 actions per day - roughly 52 days for the current backlog, throughout which
/// a container created today stays invisible.
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

        var emptyContainers = await BuildCandidateQuery(containerIdsWithTemplate)
            .OrderBy(s => s.FromDate)
            .ThenBy(s => s.Id)
            .Take(MaxFindingsPerTick)
            .ToListAsync(cancellationToken);

        if (emptyContainers.Count == 0)
        {
            return Array.Empty<IAgentTriggerEvent>();
        }

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
