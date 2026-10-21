// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs the clone of a new scenario in its fixed order: resolve the source and the group scope, clone the shifts, then
/// the works (with work changes and expenses), breaks, notes and commands of the window widened by the boundary days,
/// then the shift satellites and the rows only a source scenario has. Everything is staged on the shared context; the
/// caller commits.
/// </summary>
/// <param name="sourceResolver">Resolves and guards the source scenario</param>
/// <param name="groupScopeReader">Resolves the group hierarchy that scopes the clone</param>
/// <param name="shiftCloner">Clones the shifts</param>
/// <param name="workCloner">Clones the works with their work changes and expenses</param>
/// <param name="dayRowCloner">Clones breaks, schedule notes and schedule commands</param>
/// <param name="satelliteCloner">Clones client shift preferences, shift expenses and required qualifications</param>
/// <param name="sourceOnlyRowCloner">Copies a source scenario's temporary memberships and open replacement requests</param>

using Klacks.Api.Application.Constants;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioCloneCoordinator
{
    private readonly ScenarioCloneSourceResolver _sourceResolver;
    private readonly ScenarioGroupScopeReader _groupScopeReader;
    private readonly ScenarioShiftCloner _shiftCloner;
    private readonly ScenarioWorkCloner _workCloner;
    private readonly ScenarioDayRowCloner _dayRowCloner;
    private readonly ScenarioShiftSatelliteCloner _satelliteCloner;
    private readonly ScenarioSourceOnlyRowCloner _sourceOnlyRowCloner;

    public ScenarioCloneCoordinator(
        ScenarioCloneSourceResolver sourceResolver,
        ScenarioGroupScopeReader groupScopeReader,
        ScenarioShiftCloner shiftCloner,
        ScenarioWorkCloner workCloner,
        ScenarioDayRowCloner dayRowCloner,
        ScenarioShiftSatelliteCloner satelliteCloner,
        ScenarioSourceOnlyRowCloner sourceOnlyRowCloner)
    {
        _sourceResolver = sourceResolver;
        _groupScopeReader = groupScopeReader;
        _shiftCloner = shiftCloner;
        _workCloner = workCloner;
        _dayRowCloner = dayRowCloner;
        _satelliteCloner = satelliteCloner;
        _sourceOnlyRowCloner = sourceOnlyRowCloner;
    }

    public async Task<(Dictionary<Guid, Guid> ShiftIdMap, Dictionary<Guid, Guid> WorkIdMap)> CloneScenarioDataWithMapsAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, Guid? sourceToken, CancellationToken ct)
    {
        var (source, scopeGroupId) = await _sourceResolver.ResolveCloneSourceAsync(sourceToken, groupId, ct);

        var groupIds = scopeGroupId.HasValue
            ? await _groupScopeReader.GetGroupHierarchyIdsAsync(scopeGroupId.Value, ct)
            : null;

        var boundaryFrom = fromDate.AddDays(-ScenarioConstants.BoundaryDays);
        var boundaryUntil = untilDate.AddDays(ScenarioConstants.BoundaryDays);

        var (shiftIdMap, clonedShifts) = await _shiftCloner.CloneShifts(groupIds, additionalShiftIds, token, source, ct);
        var workShiftIdMap = ScenarioShiftCloner.WithRootAliases(clonedShifts, shiftIdMap, source);
        var workIdMap = await _workCloner.CloneWorks(groupIds, boundaryFrom, boundaryUntil, token, workShiftIdMap, source, ct);
        await _dayRowCloner.CloneBreaks(groupIds, boundaryFrom, boundaryUntil, token, workIdMap, source, ct);
        await _dayRowCloner.CloneScheduleNotes(groupIds, boundaryFrom, boundaryUntil, token, source, ct);
        await _dayRowCloner.CloneScheduleCommands(groupIds, boundaryFrom, boundaryUntil, token, source, ct);
        await _satelliteCloner.CloneClientShiftPreferences(shiftIdMap, token, source, ct);
        await _satelliteCloner.CloneShiftExpenses(shiftIdMap, token, source, ct);
        await _satelliteCloner.CloneShiftRequiredQualifications(shiftIdMap, token, ct);
        await _sourceOnlyRowCloner.CloneScenarioMemberships(source, token, ct);
        await _sourceOnlyRowCloner.CloneOpenReplacementRequests(source, token, ct);

        return (workShiftIdMap, workIdMap);
    }
}
