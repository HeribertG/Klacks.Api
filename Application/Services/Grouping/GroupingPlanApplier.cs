// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Applies an approved grouping plan inside one database transaction. The new group (if any) is created
/// first, before anything is staged, because the group repository commits on its own and would
/// otherwise flush staged rows early. Then shift and client memberships are staged, saved and verified;
/// only afterwards are dead memberships soft-deleted and their removal verified. Memberships that
/// already exist (or are already gone) are counted as in place. Any verification mismatch throws a
/// SkillVerificationException, which rolls the whole transaction back. A plan that targets the new group
/// without proposing it, or proposes it without a name, is rejected before the transaction starts.
/// </summary>
/// <param name="groupRepository">Creates the new group (nested set, visibility preservation; self-committing).</param>
/// <param name="groupItemRepository">Stages and soft-deletes group_item rows; counts them for verification.</param>
/// <param name="unitOfWork">Transaction and SaveChanges.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Interfaces.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Models.Associations;

namespace Klacks.Api.Application.Services.Grouping;

public sealed class GroupingPlanApplier : IGroupingPlanApplier
{
    private const string MissingGroupNameMessage = "A name for the new group is required to apply this plan.";
    private const string MissingCreateGroupMessage =
        "The plan targets a new group but does not propose to create it.";
    private const string GroupNotConfirmedMessage =
        "Database verification failed: the new group '{0}' could not be confirmed — the changes were rolled back.";
    private const string AddsNotConfirmedMessage =
        "Database verification failed: expected {0} new group memberships but only {1} were confirmed — the changes were rolled back.";
    private const string RemovalsNotConfirmedMessage =
        "Database verification failed: {0} removed group memberships are still active — the changes were rolled back.";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public GroupingPlanApplier(IGroupRepository groupRepository, IGroupItemRepository groupItemRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public async Task<GroupingApplyResult> ApplyAsync(GroupingApplyCommand command, CancellationToken cancellationToken)
    {
        var proposals = command.Proposals;
        var needsGroup = proposals.Any(proposal => proposal.Kind == GroupingProposalKind.CreateGroup);
        if (needsGroup && string.IsNullOrWhiteSpace(command.NewGroupName))
        {
            throw new ArgumentException(MissingGroupNameMessage, nameof(command));
        }

        if (!needsGroup && proposals.Any(proposal => proposal.Kind != GroupingProposalKind.CreateGroup && proposal.GroupId is null))
        {
            throw new ArgumentException(MissingCreateGroupMessage, nameof(command));
        }

        return await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            var createdGroupId = needsGroup ? await CreateGroupAsync(command) : (Guid?)null;
            var now = DateTime.UtcNow;
            var newItems = new List<GroupItem>();
            var alreadyInPlace = 0;

            foreach (var proposal in proposals.Where(p => p.Kind is GroupingProposalKind.AddShift or GroupingProposalKind.AddClient))
            {
                var groupId = proposal.GroupId ?? createdGroupId!.Value;
                if (await IsAlreadyMemberAsync(proposal, groupId, cancellationToken))
                {
                    alreadyInPlace++;
                    continue;
                }

                var item = new GroupItem
                {
                    Id = Guid.NewGuid(),
                    GroupId = groupId,
                    ClientId = proposal.Kind == GroupingProposalKind.AddClient ? proposal.ClientId : null,
                    ShiftId = proposal.Kind == GroupingProposalKind.AddShift ? proposal.ShiftId : null,
                    ValidFrom = command.ValidFromUtc,
                    CreateTime = now,
                    CurrentUserCreated = command.UserName
                };
                await _groupItemRepository.Add(item);
                newItems.Add(item);
            }

            await _unitOfWork.CompleteAsync();
            await VerifyAddedAsync(newItems, cancellationToken);

            var removedIds = new List<Guid>();
            foreach (var proposal in proposals.Where(p => p.Kind == GroupingProposalKind.RemoveClient))
            {
                var existing = await _groupItemRepository.GetByClientAndGroup(proposal.ClientId!.Value, proposal.GroupId!.Value);
                if (existing is null || existing.IsDeleted)
                {
                    alreadyInPlace++;
                    continue;
                }

                await _groupItemRepository.Delete(existing.Id);
                removedIds.Add(existing.Id);
            }

            await _unitOfWork.CompleteAsync();
            await VerifyRemovedAsync(removedIds, cancellationToken);

            return new GroupingApplyResult(
                createdGroupId,
                needsGroup ? 1 : 0,
                newItems.Count(item => item.ShiftId != null),
                newItems.Count(item => item.ClientId != null),
                removedIds.Count,
                alreadyInPlace);
        });
    }

    private async Task<Guid> CreateGroupAsync(GroupingApplyCommand command)
    {
        var group = new Group
        {
            Id = Guid.NewGuid(),
            Name = command.NewGroupName!.Trim(),
            Description = string.Empty,
            ValidFrom = command.ValidFromUtc,
            PaymentInterval = PaymentInterval.Monthly,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = command.UserName
        };

        await _groupRepository.Add(group);
        if (!await _groupRepository.Exists(group.Id))
        {
            throw new SkillVerificationException(GroupingSkillNames.Apply, string.Format(GroupNotConfirmedMessage, group.Name));
        }

        return group.Id;
    }

    private async Task<bool> IsAlreadyMemberAsync(GroupingProposal proposal, Guid groupId, CancellationToken cancellationToken)
    {
        if (proposal.Kind == GroupingProposalKind.AddShift)
        {
            var groupIds = await _groupItemRepository.GetGroupIdsByShiftId(proposal.ShiftId!.Value, cancellationToken);
            return groupIds.Contains(groupId);
        }

        var existing = await _groupItemRepository.GetByClientAndGroup(proposal.ClientId!.Value, groupId);
        return existing is not null && !existing.IsDeleted;
    }

    private async Task VerifyAddedAsync(IReadOnlyList<GroupItem> newItems, CancellationToken cancellationToken)
    {
        if (newItems.Count == 0)
        {
            return;
        }

        var confirmed = await _groupItemRepository.CountExistingByIds(newItems.Select(item => item.Id).ToList(), cancellationToken);
        if (confirmed != newItems.Count)
        {
            throw new SkillVerificationException(GroupingSkillNames.Apply, string.Format(AddsNotConfirmedMessage, newItems.Count, confirmed));
        }
    }

    private async Task VerifyRemovedAsync(IReadOnlyList<Guid> removedIds, CancellationToken cancellationToken)
    {
        if (removedIds.Count == 0)
        {
            return;
        }

        var stillActive = await _groupItemRepository.CountExistingByIds(removedIds, cancellationToken);
        if (stillActive != 0)
        {
            throw new SkillVerificationException(GroupingSkillNames.Apply, string.Format(RemovalsNotConfirmedMessage, stillActive));
        }
    }
}
