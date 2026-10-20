// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Groups;
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

    /// <summary>
    /// Stages the pull-forward of existing memberships of the target groups to the employment entry date (EntryDate mode
    /// only; Fixed mode and ineligible memberships are never touched): one batched update of the memberships and one lowering
    /// of the groups that would otherwise start after their members. Writes nothing for a preview - the caller uses it only on
    /// apply, inside its transaction, commits through IUnitOfWork and then calls VerifyPullForwardAsync.
    /// </summary>
    /// <param name="plan">The start plan of the run</param>
    /// <param name="existingMembers">The existing memberships the run found, with item and group ids for those that may be changed</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<PullForwardBatch> StagePullForwardAsync(
        GroupMembershipStartPlan plan,
        IEnumerable<ExistingMembershipStart> existingMembers,
        CancellationToken cancellationToken);

    /// <summary>
    /// Re-reads the staged memberships after the commit and throws a SkillVerificationException (rolling the transaction
    /// back) when one does not start on its new date.
    /// </summary>
    /// <param name="batch">What StagePullForwardAsync returned</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task VerifyPullForwardAsync(PullForwardBatch batch, CancellationToken cancellationToken);
}
