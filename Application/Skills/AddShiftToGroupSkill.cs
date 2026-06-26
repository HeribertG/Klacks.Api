// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Skill that links an existing shift/order to a group via a group_item row (shift_id + group_id).
/// Mirrors add_client_to_group but for shifts; rejects a shift that is already in the target group.
/// </summary>
/// <param name="shiftId">UUID of the shift/order to add to the group.</param>
/// <param name="groupId">UUID of the target group.</param>
/// <param name="validFrom">Optional ISO date the membership starts; defaults to today.</param>
/// <param name="validUntil">Optional ISO date the membership ends; open-ended if omitted.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.Associations;
using Klacks.Api.Domain.Interfaces.Schedules;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("add_shift_to_group")]
public class AddShiftToGroupSkill : BaseSkillImplementation
{
    private const string SkillName = "add_shift_to_group";

    private readonly IShiftRepository _shiftRepository;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupItemRepository _groupItemRepository;
    private readonly IUnitOfWork _unitOfWork;

    public AddShiftToGroupSkill(
        IShiftRepository shiftRepository,
        IGroupRepository groupRepository,
        IGroupItemRepository groupItemRepository,
        IUnitOfWork unitOfWork)
    {
        _shiftRepository = shiftRepository;
        _groupRepository = groupRepository;
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
        var validFromStr = GetParameter<string>(parameters, "validFrom");
        var validUntilStr = GetParameter<string>(parameters, "validUntil");

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

        var existingGroupIds = await _groupItemRepository.GetGroupIdsByShiftId(shiftId, cancellationToken);
        if (existingGroupIds.Contains(groupId))
        {
            return SkillResult.Error($"Shift is already assigned to group '{group.Name}'.");
        }

        DateTime? validFrom = null;
        if (!string.IsNullOrEmpty(validFromStr) && DateTime.TryParse(validFromStr, out var parsedFrom))
        {
            validFrom = DateTime.SpecifyKind(parsedFrom, DateTimeKind.Utc);
        }

        DateTime? validUntil = null;
        if (!string.IsNullOrEmpty(validUntilStr) && DateTime.TryParse(validUntilStr, out var parsedUntil))
        {
            validUntil = DateTime.SpecifyKind(parsedUntil, DateTimeKind.Utc);
        }

        var groupItem = new GroupItem
        {
            Id = Guid.NewGuid(),
            ShiftId = shiftId,
            GroupId = groupId,
            ValidFrom = validFrom ?? DateTime.UtcNow,
            ValidUntil = validUntil,
            CreateTime = DateTime.UtcNow,
            CurrentUserCreated = context.UserName
        };

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _groupItemRepository.Add(groupItem);
                await _unitOfWork.CompleteAsync();
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _groupItemRepository.GetNoTracking(groupItem.Id),
                    persisted => !persisted.IsDeleted
                        && persisted.ShiftId == shiftId
                        && persisted.GroupId == groupId,
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
            GroupItemId = groupItem.Id,
            ShiftId = shiftId,
            GroupId = groupId,
            GroupName = group.Name,
            ValidFrom = groupItem.ValidFrom,
            ValidUntil = groupItem.ValidUntil,
            Verified = true
        };

        return SkillResult.SuccessResult(
            resultData,
            $"Shift successfully added to group '{group.Name}' and confirmed in the database (verified).");
    }
}
