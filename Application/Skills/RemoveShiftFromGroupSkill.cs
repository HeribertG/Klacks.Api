// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that unlinks a shift/order from a group by deleting its group_item row (shift_id + group_id).
/// Inverse of add_shift_to_group; rejects a shift that is not currently linked to the target group.
/// </summary>
/// <param name="shiftId">UUID of the shift/order to remove from the group.</param>
/// <param name="groupId">UUID of the group to remove the shift from.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;
using Microsoft.EntityFrameworkCore;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("remove_shift_from_group")]
public class RemoveShiftFromGroupSkill : BaseSkillImplementation
{
    private const string SkillName = "remove_shift_from_group";

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public RemoveShiftFromGroupSkill(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _groupItemRepository = groupItemRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var shiftIdStr = GetRequiredString(parameters, "shiftId");
        var groupIdStr = GetRequiredString(parameters, "groupId");

        if (!Guid.TryParse(shiftIdStr, out var shiftId))
        {
            return SkillResult.Error($"Invalid shift ID format: {shiftIdStr}");
        }

        if (!Guid.TryParse(groupIdStr, out var groupId))
        {
            return SkillResult.Error($"Invalid group ID format: {groupIdStr}");
        }

        if (!await _shiftRepository.Exists(shiftId))
        {
            return SkillResult.Error($"Shift with ID {shiftId} not found.");
        }

        var group = await _groupRepository.Get(groupId);
        if (group == null)
        {
            return SkillResult.Error($"Group with ID {groupId} not found.");
        }

        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        if (!scope.IsInScope(group))
        {
            return SkillResult.Error(scope.BuildOutOfScopeError(group.Name));
        }

        var groupItem = await _groupItemRepository.GetQuery()
            .FirstOrDefaultAsync(gi => gi.ShiftId == shiftId && gi.GroupId == groupId, cancellationToken);
        if (groupItem == null)
        {
            var otherGroupIds = await _groupItemRepository.GetGroupIdsByShiftId(shiftId, cancellationToken);
            var suffix = otherGroupIds.Count > 0
                ? $" It is currently linked to {otherGroupIds.Count} other group(s)."
                : string.Empty;
            return SkillResult.Error($"Shift is not linked to group '{group.Name}'.{suffix}");
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _groupItemRepository.Delete(groupItem.Id);
                await _unitOfWork.CompleteAsync();
                await ConfirmDeletedAsync(
                    SkillName,
                    () => _groupItemRepository.GetNoTracking(groupItem.Id),
                    $"the link of the shift to group '{group.Name}'");
                return groupItem.Id;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var resultData = new
        {
            ShiftId = shiftId,
            GroupId = groupId,
            GroupName = group.Name,
            Verified = true
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Shift removed from group '{group.Name}' and confirmed in the database (verified).");
    }
}
