// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Reads the effective scheduling policy of every staff member (employee or external, not customer) of a group, so a
/// split can be judged against the rules that hang on each member's contract.
/// </summary>
public interface IGroupMemberPolicyReader
{
    Task<IReadOnlyList<SchedulingPolicy>> GetMemberPoliciesAsync(
        Guid groupId, DateOnly date, CancellationToken cancellationToken = default);
}
