// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Takes the people who are only borrowed into the planning group out of the agents of a planning run: whoever holds a
/// temporary membership of the real plan in the group or one of its subgroups and no regular membership there
/// (IGroupItemRepository.GetBorrowedOnlyClientIdsAsync). The group of the run is the group its scenarios belong to
/// (ScenarioGroupInheritance: the requested group, else the group of the source scenario); its whole subtree counts,
/// as in the clone scope of a scenario, which copies the absences of the regular members only. A borrowed person is
/// planned in her home group: in the receiving group her absences are missing from the scenario. Everybody else stays,
/// whatever brought them into the list, and the order of the rest is kept (the roster order of the planner is a rank).
/// A run without a group and without a source scenario reads the real plan with every absence and is left alone.
/// </summary>
/// <param name="scenarioRepository">Reads the source scenario whose group stands in for a run without a group</param>
/// <param name="groupClient">Resolves the subtree of the planning group</param>
/// <param name="groupItemRepository">Reads who is only borrowed into the subtree</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Associations;

namespace Klacks.Api.Application.Services.Schedules;

public sealed class BorrowedAgentFilter : IBorrowedAgentFilter
{
    private readonly IAnalyseScenarioRepository _scenarioRepository;
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupClient;
    private readonly IGroupItemRepository _groupItemRepository;

    public BorrowedAgentFilter(
        IAnalyseScenarioRepository scenarioRepository,
        IGetAllClientIdsFromGroupAndSubgroups groupClient,
        IGroupItemRepository groupItemRepository)
    {
        _scenarioRepository = scenarioRepository;
        _groupClient = groupClient;
        _groupItemRepository = groupItemRepository;
    }

    public async Task<IReadOnlyList<Guid>> RemoveBorrowedAsync(
        IReadOnlyList<Guid> agentIds, Guid? groupId, Guid? analyseToken, CancellationToken cancellationToken)
    {
        if (agentIds.Count == 0)
        {
            return agentIds;
        }

        var sourceScenario = groupId is null && analyseToken is Guid token
            ? await _scenarioRepository.GetByTokenAsync(token, cancellationToken)
            : null;
        if (ScenarioGroupInheritance.Resolve(groupId, sourceScenario) is not Guid planningGroupId)
        {
            return agentIds;
        }

        var groupIds = await _groupClient.GetAllGroupIdsIncludingSubgroups(planningGroupId);
        var borrowed = await _groupItemRepository.GetBorrowedOnlyClientIdsAsync(agentIds, groupIds, cancellationToken);
        if (borrowed.Count == 0)
        {
            return agentIds;
        }

        return agentIds.Where(agentId => !borrowed.Contains(agentId)).ToList();
    }
}
