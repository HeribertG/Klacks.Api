// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupMembershipStartPlanner
{
    /// <summary>
    /// Plans the start dates of new group memberships. Fixed mode uses givenStart, else defaultStart, else the company
    /// today; EntryDate mode reads each person's employment entry in one batch and falls back to the company today.
    /// </summary>
    /// <param name="clientIds">The persons the memberships are for</param>
    /// <param name="givenStart">The start date the caller supplied; null when none</param>
    /// <param name="mode">Fixed or EntryDate</param>
    /// <param name="defaultStart">Fixed-mode start when givenStart is null; null means the company today</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<GroupMembershipStartPlan> PlanAsync(
        IReadOnlyCollection<Guid> clientIds,
        DateTime? givenStart,
        GroupMembershipStartMode mode,
        DateTime? defaultStart,
        CancellationToken cancellationToken);
}
