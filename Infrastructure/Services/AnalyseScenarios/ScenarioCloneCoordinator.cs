// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs the clone of a new scenario in its fixed order: resolve the source and the group scope, clone the shifts, then
/// the works (with work changes and expenses), breaks, notes and commands of the window widened by the boundary days,
/// then the shift satellites and the rows only a source scenario has. The employees whose breaks, notes and commands
/// are cloned are read once and handed to the day-row cloner: the regular members of the group hierarchy, the same
/// people the accept reads its scope for. The source resolver sorts the variants (keys) of the
/// source once: the shift clone sees the tokens of the caller plus those of the keys inside the group hierarchy of the new
/// scenario, so a chained run counts key children in its snapshot like the source did, and the new scenario receives the
/// links of those keys. Of any other key neither the clones of the source nor their works are taken. Everything is staged
/// on the shared context; the caller commits.
/// </summary>
/// <param name="sourceResolver">Resolves and guards the source scenario and sorts the variants it carries</param>
/// <param name="groupScopeReader">Resolves the group hierarchy that scopes the clone</param>
/// <param name="shiftCloner">Clones the shifts</param>
/// <param name="workCloner">Clones the works with their work changes and expenses</param>
/// <param name="dayRowCloner">Clones breaks, schedule notes and schedule commands</param>
/// <param name="satelliteCloner">Clones client shift preferences, shift expenses and required qualifications</param>
/// <param name="sourceOnlyRowCloner">Copies a source scenario's temporary memberships and open replacement requests</param>
/// <param name="variantLinkCloner">Gives the new scenario the variant links of its source scenario inside its group hierarchy</param>

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
    private readonly ScenarioVariantLinkCloner _variantLinkCloner;

    public ScenarioCloneCoordinator(
        ScenarioCloneSourceResolver sourceResolver,
        ScenarioGroupScopeReader groupScopeReader,
        ScenarioShiftCloner shiftCloner,
        ScenarioWorkCloner workCloner,
        ScenarioDayRowCloner dayRowCloner,
        ScenarioShiftSatelliteCloner satelliteCloner,
        ScenarioSourceOnlyRowCloner sourceOnlyRowCloner,
        ScenarioVariantLinkCloner variantLinkCloner)
    {
        _sourceResolver = sourceResolver;
        _groupScopeReader = groupScopeReader;
        _shiftCloner = shiftCloner;
        _workCloner = workCloner;
        _dayRowCloner = dayRowCloner;
        _satelliteCloner = satelliteCloner;
        _sourceOnlyRowCloner = sourceOnlyRowCloner;
        _variantLinkCloner = variantLinkCloner;
    }

    public async Task<(Dictionary<Guid, Guid> ShiftIdMap, Dictionary<Guid, Guid> WorkIdMap)> CloneScenarioDataWithMapsAsync(Guid? groupId, DateOnly fromDate, DateOnly untilDate, Guid token, IReadOnlyCollection<Guid>? additionalShiftIds, Guid? sourceToken, CancellationToken ct, IReadOnlySet<Guid>? variantTokens = null)
    {
        var (source, scopeGroupId) = await _sourceResolver.ResolveCloneSourceAsync(sourceToken, groupId, ct);

        var groupIds = scopeGroupId.HasValue
            ? await _groupScopeReader.GetGroupHierarchyIdsAsync(scopeGroupId.Value, ct)
            : null;

        var variants = await _sourceResolver.ResolveCloneVariantsAsync(source, variantTokens, groupIds, ct);

        var boundaryFrom = fromDate.AddDays(-ScenarioConstants.BoundaryDays);
        var boundaryUntil = untilDate.AddDays(ScenarioConstants.BoundaryDays);

        var (shiftIdMap, clonedShifts, leftBehindShiftIds) = await _shiftCloner.CloneShifts(groupIds, additionalShiftIds, token, source, variants, ct);
        var workShiftIdMap = ScenarioShiftCloner.WithRootAliases(clonedShifts, shiftIdMap, source);
        var workIdMap = await _workCloner.CloneWorks(groupIds, boundaryFrom, boundaryUntil, token, workShiftIdMap, leftBehindShiftIds, source, ct);
        (List<Guid> Real, List<Guid> WithSource)? clientScope = groupIds is null
            ? null
            : await _groupScopeReader.LoadGroupClientScopeAsync(groupIds, source, ct);
        await _dayRowCloner.CloneBreaks(clientScope, boundaryFrom, boundaryUntil, token, workIdMap, source, ct);
        await _dayRowCloner.CloneScheduleNotes(clientScope, boundaryFrom, boundaryUntil, token, source, ct);
        await _dayRowCloner.CloneScheduleCommands(clientScope, boundaryFrom, boundaryUntil, token, source, ct);
        await _satelliteCloner.CloneClientShiftPreferences(shiftIdMap, token, source, ct);
        await _satelliteCloner.CloneShiftExpenses(shiftIdMap, token, source, ct);
        await _satelliteCloner.CloneShiftRequiredQualifications(shiftIdMap, token, ct);
        await _sourceOnlyRowCloner.CloneScenarioMemberships(source, token, ct);
        await _sourceOnlyRowCloner.CloneOpenReplacementRequests(source, token, variants.DroppedTokens, workIdMap, ct);
        await _variantLinkCloner.CloneLinksAsync(token, variants, ct);

        return (workShiftIdMap, workIdMap);
    }
}
