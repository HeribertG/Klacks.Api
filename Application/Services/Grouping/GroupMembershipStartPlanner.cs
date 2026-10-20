// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IGroupMembershipStartPlanner: resolves the fixed start (supplied, else the caller's default, else the company
/// today) and, in EntryDate mode, reads the employment window of all persons in one batch.
/// </summary>
/// <param name="membershipWindowReader">Reads Membership.ValidFrom / ValidUntil per client in one batch</param>
/// <param name="companyClock">Supplies the company-local today used as default and as fallback</param>

using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupMembershipStartPlanner : IGroupMembershipStartPlanner
{
    private readonly IMembershipWindowReader _membershipWindowReader;
    private readonly ICompanyClock _companyClock;

    public GroupMembershipStartPlanner(IMembershipWindowReader membershipWindowReader, ICompanyClock companyClock)
    {
        _membershipWindowReader = membershipWindowReader;
        _companyClock = companyClock;
    }

    public async Task<GroupMembershipStartPlan> PlanAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateTime? givenStart,
        GroupMembershipStartMode mode,
        DateTime? defaultStart,
        CancellationToken cancellationToken)
    {
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var windows = await _membershipWindowReader.GetWindowsAsync(clientIds, cancellationToken);

        return new GroupMembershipStartPlan(
            mode,
            givenStart.HasValue || mode == GroupMembershipStartMode.EntryDate,
            givenStart ?? defaultStart ?? today,
            today,
            windows);
    }
}
