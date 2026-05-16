// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Soft-deletes a group by id. Refuses if the group still has children (use MoveNode
/// for the children first or pass forceCascade=true to also soft-delete the subtree).
/// </summary>
/// <param name="groupId">Required. UUID of the group to delete.</param>
/// <param name="forceCascade">Optional. If true, soft-deletes the subtree too. Default false.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("delete_group")]
public class DeleteGroupSkill : BaseSkillImplementation
{
    private readonly IGroupRepository _groupRepository;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteGroupSkill(IGroupRepository groupRepository, IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var groupId = GetRequiredGuid(parameters, "groupId");
        var forceCascade = GetParameter<bool>(parameters, "forceCascade", false);

        var group = await _groupRepository.Get(groupId);
        if (group == null)
        {
            return SkillResult.Error($"Group with ID '{groupId}' not found.");
        }

        var children = (await _groupRepository.GetChildren(groupId)).ToList();
        if (children.Count > 0 && !forceCascade)
        {
            return SkillResult.Error(
                $"Group '{group.Name}' has {children.Count} child group(s). Pass forceCascade=true to soft-delete the subtree.");
        }

        var deletedCount = 0;
        if (forceCascade && children.Count > 0)
        {
            foreach (var child in children)
            {
                await _groupRepository.Delete(child.Id);
                deletedCount++;
            }
        }

        var groupName = group.Name;
        await _groupRepository.Delete(groupId);
        deletedCount++;
        await _unitOfWork.CompleteAsync();

        return SkillResult.SuccessResult(
            new
            {
                GroupId = groupId,
                DeletedGroupName = groupName,
                CascadeCount = deletedCount
            },
            $"Group '{groupName}' was soft-deleted ({deletedCount} row(s) total).");
    }
}
