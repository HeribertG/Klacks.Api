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
/// explicit order the cap would otherwise pick from physical storage order, an oldest-first triage
/// queue that drains as containers get a template, rather than a fixed 50 that starve out the rest.
/// </summary>
/// <param name="shiftRepository">Read-only access to container shift candidates.</param>
/// <param name="containerTemplateRepository">Read-only access to the set of container ids that already have a template.</param>
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

public class EmptyContainerDetector : IAgentTriggerDetector, IAgentConditionFingerprintSource
{
    public const int MaxFindingsPerTick = 50;

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
                ShiftGroupScope.For(groupsByShift, container.Id)))
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

    private IQueryable<Shift> BuildCandidateQuery(List<Guid> containerIdsWithTemplate) =>
        _shiftRepository.GetQuery()
            .Where(s => s.ShiftType == ShiftType.IsContainer)
            .Where(s => s.Status == ShiftStatus.OriginalShift)
            .Where(s => s.AnalyseToken == null && s.ScenarioSourceShiftId == null)
            .Where(s => !s.IsDeleted)
            .Where(s => !containerIdsWithTemplate.Contains(s.Id));
}
