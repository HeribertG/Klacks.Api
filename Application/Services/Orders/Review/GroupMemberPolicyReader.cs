// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the effective scheduling policies of a group's staff members: the members come from the group membership
/// service (customers left out), their policies from the scheduling policy resolver at the given date, which combines
/// each member's contract and scheduling rule with the global defaults.
/// </summary>
/// <param name="groupMembershipService">Lists the clients that are members of a group.</param>
/// <param name="policyResolver">Resolves the effective policy of each member at a date.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Schedules;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Services.Orders.Review;

public sealed class GroupMemberPolicyReader : IGroupMemberPolicyReader
{
    private readonly IGroupMembershipService _groupMembershipService;
    private readonly ISchedulingPolicyResolver _policyResolver;

    public GroupMemberPolicyReader(IGroupMembershipService groupMembershipService, ISchedulingPolicyResolver policyResolver)
    {
        _groupMembershipService = groupMembershipService;
        _policyResolver = policyResolver;
    }

    public async Task<IReadOnlyList<SchedulingPolicy>> GetMemberPoliciesAsync(
        Guid groupId, DateOnly date, CancellationToken cancellationToken = default)
    {
        var members = await _groupMembershipService.GetGroupMembersAsync(groupId);
        var staffIds = members
            .Where(member => !member.IsDeleted && member.Type != EntityTypeEnum.Customer)
            .Select(member => member.Id)
            .Distinct()
            .ToList();
        if (staffIds.Count == 0)
        {
            return [];
        }

        var policies = await _policyResolver.GetForClientsAsync(staffIds, date);
        return policies.Values.ToList();
    }
}
