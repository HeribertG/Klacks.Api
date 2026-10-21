// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// EF Core-backed implementation of AnalyseScenario data operations. A thin entry point over the scenario
/// collaborators of this folder, so Create / Accept / Reject / Delete handlers and the wizard, harmonizer and cover
/// paths share one source of truth for cloning, promoting, the accept checks and the soft-delete + orphan sweep. All
/// collaborators share the scope's DataBaseContext and only stage changes; the caller commits.
/// </summary>
/// <param name="cloneCoordinator">Clones the real plan (or a source scenario) into a new scenario</param>
/// <param name="groupScopeReader">Resolves group hierarchies</param>
/// <param name="rowLookup">Read-only lookups over the rows of a scenario</param>
/// <param name="ownedRowStager">Stages the temporary memberships a scenario owns</param>
/// <param name="dataDiscarder">Reject/delete sweep of a scenario</param>
/// <param name="realPlanReplacer">Accept delete step on the real plan</param>
/// <param name="stalenessDetector">Accept staleness check</param>
/// <param name="conflictValidator">Accept conflict checks</param>
/// <param name="promoter">Accept promote step</param>
/// <param name="slotReplacer">Wizard slot replacement in a scenario</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class AnalyseScenarioService : IAnalyseScenarioService
{
    private readonly ScenarioCloneCoordinator _cloneCoordinator;
    private readonly ScenarioGroupScopeReader _groupScopeReader;
    private readonly ScenarioRowLookup _rowLookup;
    private readonly ScenarioOwnedRowStager _ownedRowStager;
    private readonly ScenarioDataDiscarder _dataDiscarder;
    private readonly ScenarioRealPlanReplacer _realPlanReplacer;
    private readonly ScenarioStalenessDetector _stalenessDetector;
    private readonly ScenarioAcceptConflictValidator _conflictValidator;
    private readonly ScenarioPromoter _promoter;
    private readonly ScenarioSlotReplacer _slotReplacer;

    public AnalyseScenarioService(
        ScenarioCloneCoordinator cloneCoordinator,
        ScenarioGroupScopeReader groupScopeReader,
        ScenarioRowLookup rowLookup,
        ScenarioOwnedRowStager ownedRowStager,
        ScenarioDataDiscarder dataDiscarder,
        ScenarioRealPlanReplacer realPlanReplacer,
        ScenarioStalenessDetector stalenessDetector,
        ScenarioAcceptConflictValidator conflictValidator,
        ScenarioPromoter promoter,
        ScenarioSlotReplacer slotReplacer)
    {
        _cloneCoordinator = cloneCoordinator;
        _groupScopeReader = groupScopeReader;
        _rowLookup = rowLookup;
        _ownedRowStager = ownedRowStager;
        _dataDiscarder = dataDiscarder;
        _realPlanReplacer = realPlanReplacer;
        _stalenessDetector = stalenessDetector;
        _conflictValidator = conflictValidator;
        _promoter = promoter;
        _slotReplacer = slotReplacer;
    }

    public Task<List<Guid>> GetGroupHierarchyIdsAsync(Guid groupId, CancellationToken ct)
        => _groupScopeReader.GetGroupHierarchyIdsAsync(groupId, ct);

    public Task SoftDeleteScenarioDataAsync(Guid token, CancellationToken ct)
        => _dataDiscarder.SoftDeleteScenarioDataAsync(token, ct);

    public async Task<Dictionary<Guid, Guid>> CloneScenarioDataAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, Guid? sourceToken, CancellationToken ct)
        => (await _cloneCoordinator.CloneScenarioDataWithMapsAsync(groupId, fromDate, untilDate, token, additionalShiftIds, sourceToken, ct)).ShiftIdMap;

    public Task<(Dictionary<Guid, Guid> ShiftIdMap, Dictionary<Guid, Guid> WorkIdMap)> CloneScenarioDataWithMapsAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, Guid? sourceToken, CancellationToken ct)
        => _cloneCoordinator.CloneScenarioDataWithMapsAsync(groupId, fromDate, untilDate, token, additionalShiftIds, sourceToken, ct);

    public Task<IReadOnlyDictionary<Guid, Guid>> ResolveRootShiftIdsAsync(IReadOnlyCollection<Guid> shiftIds, CancellationToken cancellationToken)
        => _rowLookup.ResolveRootShiftIdsAsync(shiftIds, cancellationToken);

    public Task<IReadOnlySet<Guid>> GetScenarioShiftIdsWithRootsAsync(Guid token, CancellationToken cancellationToken)
        => _rowLookup.GetScenarioShiftIdsWithRootsAsync(token, cancellationToken);

    public Task AddScenarioMembershipAsync(Guid token, Guid clientId, Guid groupId, DateOnly validFrom, DateOnly validUntil, CancellationToken cancellationToken)
        => _ownedRowStager.AddScenarioMembershipAsync(token, clientId, groupId, validFrom, validUntil, cancellationToken);

    public Task SoftDeleteRealScheduleDataAsync(Guid? groupId, Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
        => _realPlanReplacer.SoftDeleteRealScheduleDataAsync(groupId, token, fromDate, untilDate, ct);

    public Task<ScenarioStaleness?> FindRealPlanChangesSinceCreationAsync(AnalyseScenario scenario, CancellationToken ct)
        => _stalenessDetector.FindRealPlanChangesSinceCreationAsync(scenario, ct);

    public Task ValidateNoAcceptConflictsAsync(Guid token, CancellationToken ct)
        => _conflictValidator.ValidateNoAcceptConflictsAsync(token, ct);

    public Task<ScenarioAcceptLockSkips> PromoteScenarioWorksAsync(Guid token, DateOnly fromDate, DateOnly untilDate, CancellationToken ct)
        => _promoter.PromoteScenarioWorksAsync(token, fromDate, untilDate, ct);

    public Task<IReadOnlyList<Guid>> GetScenarioBreakIdsAsync(Guid token, CancellationToken ct)
        => _rowLookup.GetScenarioBreakIdsAsync(token, ct);

    public Task<IReadOnlyList<Guid>> SoftDeleteClonedWorksOnSlotsAsync(Guid token, DateOnly fromDate, DateOnly untilDate, IReadOnlySet<(Guid ShiftId, DateOnly Date)> plannedSlots, CancellationToken ct)
        => _slotReplacer.SoftDeleteClonedWorksOnSlotsAsync(token, fromDate, untilDate, plannedSlots, ct);

    public Task<int> ResolveDisplacedWorksAsync(Guid token, IReadOnlyList<Guid> displacedWorkIds, IReadOnlyList<Guid> createdWorkIds, CancellationToken ct)
        => _slotReplacer.ResolveDisplacedWorksAsync(token, displacedWorkIds, createdWorkIds, ct);
}
