// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Default IGroupMembershipStartPlanner: resolves the fixed start (supplied, else the caller's default, else the company
/// today) and, in EntryDate mode, reads the employment window of all persons in one batch.
/// </summary>
/// <param name="membershipWindowReader">Reads Membership.ValidFrom / ValidUntil per client in one batch</param>
/// <param name="companyClock">Supplies the company-local today used as default and as fallback</param>
/// <param name="groupItemRepository">Pulls existing memberships forward and re-reads them for the verification, in batches</param>
/// <param name="groupRepository">Lowers the start of groups that would otherwise begin after their members</param>
/// <param name="unitOfWork">Discards the tracked changes when the verification fails, so the rolled-back start dates do not linger in the scope</param>

using System.Globalization;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupMembershipStartPlanner : IGroupMembershipStartPlanner
{
    private readonly IMembershipWindowReader _membershipWindowReader;
    private const string SkillName = "group_membership_start";

    private const string VerificationFailedMessage =
        "Database verification failed: {0} of {1} existing memberships start on their new date, {2} could not be found " +
        "(deleted meanwhile) and {3} still start later - the changes were rolled back and nothing was saved. " +
        "Run the request again.";

    private readonly ICompanyClock _companyClock;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GroupMembershipStartPlanner(
        IMembershipWindowReader membershipWindowReader,
        ICompanyClock companyClock,
        IGroupItemRepository groupItemRepository,
        IGroupRepository groupRepository,
        IUnitOfWork unitOfWork)
    {
        _unitOfWork = unitOfWork;
        _membershipWindowReader = membershipWindowReader;
        _companyClock = companyClock;
        _groupItemRepository = groupItemRepository;
        _groupRepository = groupRepository;
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
        var ended = mode == GroupMembershipStartMode.EntryDate
            ? await _groupItemRepository.GetEndedMembershipsAsync(clientIds, cancellationToken)
            : Array.Empty<(Guid ClientId, Guid GroupId, DateTime ValidUntil)>();

        return new GroupMembershipStartPlan(
            mode,
            givenStart.HasValue || mode == GroupMembershipStartMode.EntryDate,
            givenStart ?? defaultStart ?? today,
            today,
            windows,
            ended.ToLookup(entry => entry.ClientId, entry => (entry.GroupId, entry.ValidUntil)));
    }

    public async Task<PullForwardBatch> StagePullForwardAsync(
        GroupMembershipStartPlan plan,
        IEnumerable<ExistingMembershipStart> existingMembers,
        CancellationToken cancellationToken)
    {
        var candidates = plan.PullForwardCandidates(existingMembers);
        if (candidates.Count == 0)
        {
            return PullForwardBatch.Empty;
        }

        var itemStarts = candidates.ToDictionary(candidate => candidate.ItemId, candidate => candidate.NewStart);
        var groupStarts = candidates
            .GroupBy(candidate => candidate.GroupId)
            .ToDictionary(group => group.Key, group => EarlierOf(group.Min(candidate => candidate.NewStart), plan.Today));

        var changed = await _groupItemRepository.PullForwardValidFromAsync(itemStarts, cancellationToken);
        await _groupRepository.LowerValidFromAsync(groupStarts, cancellationToken);

        return new PullForwardBatch(itemStarts, groupStarts, changed);
    }

    public async Task VerifyPullForwardAsync(PullForwardBatch batch, CancellationToken cancellationToken)
    {
        if (batch.Count == 0)
        {
            return;
        }

        var current = await _groupItemRepository.GetValidFromByIdsAsync(batch.ItemStarts.Keys.ToList(), cancellationToken);
        var confirmed = batch.ItemStarts.Count(item =>
            current.TryGetValue(item.Key, out var start) && start.HasValue && start.Value <= item.Value);
        if (confirmed != batch.Count)
        {
            var missing = batch.ItemStarts.Count(item => !current.ContainsKey(item.Key));
            _unitOfWork.DiscardTrackedChanges();
            throw new SkillVerificationException(
                SkillName,
                string.Format(
                    CultureInfo.InvariantCulture, VerificationFailedMessage,
                    confirmed, batch.Count, missing, batch.Count - confirmed - missing));
        }
    }

    private static DateTime EarlierOf(DateTime left, DateTime right) => left < right ? left : right;
}
