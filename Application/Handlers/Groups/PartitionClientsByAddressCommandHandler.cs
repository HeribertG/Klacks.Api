// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands.Groups;
using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Groups;

/// <summary>
/// Handler for <see cref="PartitionClientsByAddressCommand"/>. Delegates the region/canton/city plan to
/// the pure <see cref="GroupPartitionPlanner"/>, then with Apply=false only returns the plan; with
/// Apply=true creates the missing groups (each via <see cref="IGroupRepository.Add"/>, top-down so a
/// parent's nested-set row is committed before its children are inserted) and the new memberships in a
/// single transaction, committing once and re-reading the memberships to confirm the write.
/// </summary>
/// <param name="clientRepository">Loads clients of the requested entity type with their addresses and group memberships.</param>
/// <param name="groupRepository">Provides the existing groups and creates the missing ones (nested-set aware).</param>
/// <param name="groupItemRepository">Reads existing memberships and adds new ones.</param>
/// <param name="unitOfWork">Commits and verifies the new memberships in a single transaction.</param>
/// <param name="companyClock">Supplies the company-local date used when no explicit ValidFrom is given.</param>
public sealed class PartitionClientsByAddressCommandHandler
    : IRequestHandler<PartitionClientsByAddressCommand, PartitionClientsByAddressResult>
{
    private const string SkillName = "partition_clients_by_address";
    private const int MaxUnassignableSample = 20;

    private readonly IClientRepository _clientRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;

    public PartitionClientsByAddressCommandHandler(
        IClientRepository clientRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock)
    {
        _clientRepository = clientRepository;
        _groupRepository = groupRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
    }

    public async Task<PartitionClientsByAddressResult> Handle(
        PartitionClientsByAddressCommand request, CancellationToken cancellationToken)
    {
        var clients = await _clientRepository.GetByTypeWithAddressesAndGroupItemsAsync(
            request.EntityType, cancellationToken);
        var existingGroups = (await _groupRepository.List()).ToList();

        var plan = GroupPartitionPlanner.Plan(
            clients, existingGroups, request.Level, request.RootGroupId, request.IncludeAlreadyGrouped);

        if (!request.Apply)
        {
            return BuildResult(
                request, plan, applied: false, groups: plan.Groups, assignedCount: 0, verifiedCount: 0, alreadyMemberCount: 0);
        }

        var validFrom = request.ValidFrom ?? await _companyClock.GetTodayAsync(cancellationToken);
        var now = DateTime.UtcNow;
        var alreadyMember = 0;
        var resolvedGroupIds = new Dictionary<string, Guid>(StringComparer.Ordinal);

        var verified = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            foreach (var planned in plan.Groups)
            {
                if (planned.Existed)
                {
                    resolvedGroupIds[planned.Key] = planned.ExistingGroupId!.Value;
                    continue;
                }

                var parentId = planned.ParentKey is null ? request.RootGroupId : resolvedGroupIds[planned.ParentKey];
                var group = new Group
                {
                    Id = Guid.NewGuid(),
                    Name = planned.Name,
                    Description = string.Empty,
                    Parent = parentId,
                    ValidFrom = validFrom,
                    PaymentInterval = PaymentInterval.Monthly,
                    CreateTime = now,
                    CurrentUserCreated = request.UserName
                };

                await _groupRepository.Add(group);
                resolvedGroupIds[planned.Key] = group.Id;
            }

            var newItems = new List<GroupItem>();
            foreach (var assignment in plan.Assignments)
            {
                var groupId = resolvedGroupIds[assignment.LeafGroupKey];
                var existing = await _groupItemRepository.GetByClientAndGroup(assignment.ClientId, groupId);
                if (existing != null && !existing.IsDeleted)
                {
                    alreadyMember++;
                    continue;
                }

                newItems.Add(new GroupItem
                {
                    Id = Guid.NewGuid(),
                    ClientId = assignment.ClientId,
                    GroupId = groupId,
                    ValidFrom = validFrom,
                    CreateTime = now,
                    CurrentUserCreated = request.UserName
                });
            }

            if (newItems.Count == 0)
            {
                return 0;
            }

            foreach (var item in newItems)
            {
                await _groupItemRepository.Add(item);
            }

            await _unitOfWork.CompleteAsync();

            var confirmed = await _groupItemRepository.CountExistingByIds(
                newItems.Select(i => i.Id).ToList(), cancellationToken);
            if (confirmed != newItems.Count)
            {
                throw new SkillVerificationException(
                    SkillName,
                    $"Database verification failed: expected {newItems.Count} new memberships but only " +
                    $"{confirmed} were confirmed — the changes were rolled back.");
            }

            return confirmed;
        });

        var createdGroups = plan.Groups
            .Select(g => g with { Existed = true, ExistingGroupId = resolvedGroupIds[g.Key] })
            .ToList();
        var assignedCount = plan.Assignments.Count - alreadyMember;

        return BuildResult(
            request, plan, applied: true, groups: createdGroups,
            assignedCount: assignedCount, verifiedCount: verified, alreadyMemberCount: alreadyMember);
    }

    private static PartitionClientsByAddressResult BuildResult(
        PartitionClientsByAddressCommand request,
        GroupPartitionPlan plan,
        bool applied,
        IReadOnlyList<PlannedPartitionGroup> groups,
        int assignedCount,
        int verifiedCount,
        int alreadyMemberCount) =>
        new(
            Applied: applied,
            Level: request.Level.ToString(),
            EntityType: request.EntityType.ToString(),
            TotalClients: plan.TotalClients,
            SkippedAlreadyGroupedCount: plan.SkippedAlreadyGroupedCount,
            UnassignableCount: plan.Unassignable.Count,
            AssignedCount: assignedCount,
            VerifiedCount: verifiedCount,
            AlreadyMemberCount: alreadyMemberCount,
            Groups: BuildSummaries(groups, request.RootGroupName),
            UnassignableSample: plan.Unassignable.Take(MaxUnassignableSample).ToList(),
            Warnings: plan.Warnings);

    private static List<PartitionGroupSummary> BuildSummaries(
        IReadOnlyList<PlannedPartitionGroup> groups, string? rootGroupName)
    {
        var nameByKey = groups.ToDictionary(g => g.Key, g => g.Name, StringComparer.Ordinal);

        return groups
            .Select(g => new PartitionGroupSummary(
                g.Name,
                g.ParentKey is null ? rootGroupName : nameByKey[g.ParentKey],
                g.Existed,
                g.ExistingGroupId,
                g.ClientCount))
            .ToList();
    }
}
