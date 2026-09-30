// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Commands.Grouping;
using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Services.Grouping;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.Grouping;

/// <summary>
/// Handler for <see cref="AssignShiftsToCityGroupsCommand"/>. Delegates the placement decision to the pure
/// <see cref="ShiftCityGroupPlanner"/>; with Apply=false it only returns the plan, with Apply=true it
/// removes the replaced links and creates one city-group link per planned shift in a single transaction,
/// then re-reads both sides and rolls everything back when the database does not match the plan.
/// </summary>
/// <param name="shiftRepository">Loads the shifts with their customer, its addresses and all group links.</param>
/// <param name="groupRepository">Provides the group tree the placement is matched against.</param>
/// <param name="addressRepository">Provides the per-city address positions used as city-group locations.</param>
/// <param name="groupItemRepository">Removes the replaced links, adds the new ones and confirms both after the commit.</param>
/// <param name="unitOfWork">Commits and verifies the moves in a single transaction.</param>
/// <param name="companyClock">Supplies the company-local date that decides which customer memberships are active.</param>
public sealed class AssignShiftsToCityGroupsCommandHandler
    : IRequestHandler<AssignShiftsToCityGroupsCommand, AssignShiftsToCityGroupsResult>
{
    private const string SkillName = "assign_shifts_to_city_groups";
    private const int MaxSample = 20;

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IAddressRepository _addressRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;
    private readonly ICompanyClock _companyClock;

    public AssignShiftsToCityGroupsCommandHandler(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IAddressRepository addressRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork,
        ICompanyClock companyClock)
    {
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _addressRepository = addressRepository;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
        _companyClock = companyClock;
    }

    public async Task<AssignShiftsToCityGroupsResult> Handle(
        AssignShiftsToCityGroupsCommand request, CancellationToken cancellationToken)
    {
        var shifts = await _shiftRepository.GetShiftsForCityGroupPlacementAsync(
            request.CustomerName, cancellationToken);
        var groups = (await _groupRepository.List()).ToList();
        var centroids = await _addressRepository.GetCityCentroidsAsync(cancellationToken);
        var today = await _companyClock.GetTodayAsync(cancellationToken);
        var plan = ShiftCityGroupPlanner.Plan(shifts, groups, centroids, today, request.MaxCount);

        if (!request.Apply || plan.Assignments.Count == 0)
        {
            return BuildResult(plan, applied: request.Apply, verifiedCount: 0);
        }

        var now = DateTime.UtcNow;
        var replacedIds = plan.Assignments.SelectMany(a => a.ReplacedGroupItemIds).Distinct().ToList();

        var verified = await _unitOfWork.ExecuteInTransactionAsync(async () =>
        {
            await _groupItemRepository.RemoveByIdsAsync(replacedIds, cancellationToken);

            var newItems = plan.Assignments
                .Select(assignment => new GroupItem
                {
                    Id = Guid.NewGuid(),
                    ShiftId = assignment.ShiftId,
                    GroupId = assignment.GroupId,
                    ValidFrom = assignment.ValidFrom,
                    ValidUntil = assignment.ValidUntil,
                    CreateTime = now,
                    CurrentUserCreated = request.UserName
                })
                .ToList();

            foreach (var item in newItems)
            {
                await _groupItemRepository.Add(item);
            }

            await _unitOfWork.CompleteAsync();

            var confirmed = await _groupItemRepository.CountExistingByIds(
                newItems.Select(i => i.Id).ToList(), cancellationToken);
            var remaining = await _groupItemRepository.CountExistingByIds(replacedIds, cancellationToken);
            if (confirmed != newItems.Count || remaining != 0)
            {
                throw new SkillVerificationException(
                    SkillName,
                    $"Database verification failed: expected {newItems.Count} new city-group link(s) and " +
                    $"{replacedIds.Count} removed link(s), but found {confirmed} new and {remaining} old link(s) " +
                    "still active — the changes were rolled back.");
            }

            return confirmed;
        });

        return BuildResult(plan, applied: true, verifiedCount: verified);
    }

    private static AssignShiftsToCityGroupsResult BuildResult(ShiftCityGroupPlan plan, bool applied, int verifiedCount)
    {
        var targets = plan.Assignments
            .GroupBy(a => (a.GroupId, a.GroupName))
            .Select(byGroup => new ShiftCityGroupTargetSummary(byGroup.Key.GroupName, byGroup.Key.GroupId, byGroup.Count()))
            .OrderByDescending(t => t.ShiftCount)
            .ThenBy(t => t.GroupName, StringComparer.Ordinal)
            .ToList();

        return new AssignShiftsToCityGroupsResult(
            Applied: applied,
            TotalShifts: plan.TotalShifts,
            SkippedAlreadyInCityGroupCount: plan.SkippedAlreadyInCityGroupCount,
            AssignedCount: plan.Assignments.Count,
            ReplacedLinkCount: plan.Assignments.Sum(a => a.ReplacedGroupItemIds.Count),
            VerifiedCount: verifiedCount,
            UnassignableCount: plan.Unassignable.Count,
            Targets: targets,
            AssignmentSample: plan.Assignments.Take(MaxSample).ToList(),
            UnassignableSample: plan.Unassignable.Take(MaxSample).ToList(),
            UnlocatedCityGroupNames: plan.UnlocatedCityGroupNames);
    }
}
