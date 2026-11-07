// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Bridges the Contracts IClientGroupReader to the Core IGroupMembershipService.
/// Delegates to GetGroupHierarchyMembersAsync so that clients from nested subgroups are
/// included automatically. Only messageable clients are returned (ClientTypeRules.MessageableTypes), so a broadcast
/// audience never contains a stand-in.
/// </summary>
/// <param name="membershipService">Core group membership service</param>

using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Services.Clients;
using Klacks.Plugin.Contracts;

namespace Klacks.Api.Infrastructure.Plugins;

public class ClientGroupReaderBridge : IClientGroupReader
{
    private readonly IGroupMembershipService _membershipService;

    public ClientGroupReaderBridge(IGroupMembershipService membershipService)
    {
        _membershipService = membershipService;
    }

    public async Task<IReadOnlyList<Guid>> GetClientIdsInGroupAsync(Guid groupId, CancellationToken ct = default)
    {
        var members = await _membershipService.GetGroupHierarchyMembersAsync(groupId);
        return members
            .Where(c => !c.IsDeleted && ClientTypeRules.IsMessageable(c.Type))
            .Select(c => c.Id)
            .Distinct()
            .ToList();
    }
}
