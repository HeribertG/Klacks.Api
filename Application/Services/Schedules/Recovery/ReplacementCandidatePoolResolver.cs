// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves who can stand in for a group, for the automatic absence cover and for the list of alternatives alike: the
/// active people under the root of the group who can take over a replacement (ReplacementPersonRule), split into the
/// members of the group's own subtree and everybody else under the root. The client repository filters on the group
/// root, so the root is resolved first and the root-wide result is then split. Membership means a membership of the
/// real plan: a temporary membership of a scenario does not turn a borrowed person into a member.
/// </summary>
/// <param name="clientRepository">Reads the active clients under the root</param>
/// <param name="groupRepository">Resolves the root of the group</param>
/// <param name="groupClientService">Reads the real members of the group's subtree</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Services.Schedules;

namespace Klacks.Api.Application.Services.Schedules.Recovery;

public sealed class ReplacementCandidatePoolResolver : IReplacementCandidatePoolResolver
{
    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGetAllClientIdsFromGroupAndSubgroups _groupClientService;

    public ReplacementCandidatePoolResolver(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGetAllClientIdsFromGroupAndSubgroups groupClientService)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupClientService = groupClientService;
    }

    public async Task<ReplacementCandidatePool?> ResolveAsync(Guid groupId, CancellationToken cancellationToken)
    {
        var group = await _groupRepository.Get(groupId);
        if (group is null)
        {
            return null;
        }

        var rootGroupId = group.Root ?? groupId;
        var underRoot = await _clientRepository.GetActiveClientsWithAddressesForGroupsAsync([rootGroupId], cancellationToken);
        var memberIds = (await _groupClientService.GetRealClientIdsFromGroupAndSubgroups(groupId, cancellationToken)).ToHashSet();
        var people = ReplacementPersonRule.EligibleOnly(underRoot).ToList();

        return new ReplacementCandidatePool(
            rootGroupId,
            people.Where(person => memberIds.Contains(person.Id)).ToList(),
            people.Where(person => !memberIds.Contains(person.Id)).OrderBy(person => person.Id).ToList());
    }
}
