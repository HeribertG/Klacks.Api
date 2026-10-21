// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Source selection of a scenario clone (scenario from scenario): resolves and guards the source scenario a new scenario
/// is built on. The child is accepted straight into the real plan; its source stays an independent scenario that can still
/// be accepted or rejected like any sibling scenario, with the usual accept conflict checks.
/// </summary>
/// <param name="context">Database context shared with the other scenario collaborators of the scope</param>
/// <param name="groupScopeReader">Resolves the group hierarchy of the source scenario's group</param>

using Klacks.Api.Application.Exceptions;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;
using Klacks.Api.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

public sealed class ScenarioCloneSourceResolver
{
    private const string SourceScenarioNotFoundMessage = "Source scenario with token {0} not found.";

    private const string SourceScenarioNotActiveMessage =
        "Source scenario {0} is no longer active (status {1}); a new scenario can only be built on an active scenario.";

    private const string SourceScenarioGroupScopeMessage =
        "Source scenario {0} only covers group {1} and its subgroups; a scenario built on it must stay inside that group.";

    private readonly DataBaseContext _context;
    private readonly ScenarioGroupScopeReader _groupScopeReader;

    public ScenarioCloneSourceResolver(DataBaseContext context, ScenarioGroupScopeReader groupScopeReader)
    {
        _context = context;
        _groupScopeReader = groupScopeReader;
    }

    /// <summary>
    /// Loads the source scenario and refuses one that cannot carry a child: a missing or deleted scenario, a decided one
    /// (its rows are promoted or discarded, so the clone would be empty and accepting it would wipe the real plan) and a
    /// group outside the source scenario's group hierarchy (the source holds no rows for it). A child without a group on a
    /// grouped source inherits the source's group. No visibility check: the caller must have verified that the user may
    /// see the source scenario's group (CoverAbsence does; the wizard and harmonizer apply paths are admin-only).
    /// </summary>
    /// <param name="sourceToken">Token of the scenario to build on; null = the real plan</param>
    /// <param name="groupId">Group of the new scenario; null = unscoped</param>
    /// <returns>The clone source and the group that scopes the clone</returns>
    internal async Task<(ScenarioCloneSource Source, Guid? ScopeGroupId)> ResolveCloneSourceAsync(
        Guid? sourceToken, Guid? groupId, CancellationToken ct)
    {
        if (sourceToken is not { } token)
        {
            return (ScenarioCloneSource.RealPlan, groupId);
        }

        var source = await _context.Set<AnalyseScenario>()
            .AsNoTracking()
            .FirstOrDefaultAsync(s => s.Token == token, ct)
            ?? throw new KeyNotFoundException(string.Format(SourceScenarioNotFoundMessage, token));

        if (source.Status != AnalyseScenarioStatus.Active)
        {
            throw new ScenarioNotActiveException(string.Format(SourceScenarioNotActiveMessage, source.Id, source.Status));
        }

        var scopeGroupId = groupId ?? source.GroupId;
        if (source.GroupId is { } sourceGroupId)
        {
            var sourceGroupIds = await _groupScopeReader.GetGroupHierarchyIdsAsync(sourceGroupId, ct);
            if (!sourceGroupIds.Contains(scopeGroupId!.Value))
            {
                throw new ConflictException(string.Format(SourceScenarioGroupScopeMessage, source.Id, sourceGroupId));
            }
        }

        return (new ScenarioCloneSource(token, source.FromDate, source.UntilDate), scopeGroupId);
    }
}
