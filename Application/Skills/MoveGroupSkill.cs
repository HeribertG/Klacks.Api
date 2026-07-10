// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Moves a group to a new parent in the nested-set tree (the chat equivalent of the tree
/// view's drag-and-drop). Refuses to move a group under itself or under one of its own
/// descendants, requires both the group and the new parent to be inside the caller's group
/// scope, and verifies the new parent by re-reading the group from the database inside the
/// transaction — a failed verification rolls the move back. Group and parent are addressed
/// by UUID or by name (staged name resolution with disambiguation instead of guessing).
/// </summary>
/// <param name="groupId">Optional. UUID of the group to move; takes precedence over groupName.</param>
/// <param name="groupName">Optional. Display name of the group to move.</param>
/// <param name="newParentId">Optional. UUID of the new parent group; takes precedence over newParentName.</param>
/// <param name="newParentName">Optional. Display name of the new parent group.</param>

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Exceptions;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class MoveGroupSkill : BaseSkillImplementation
{
    private const string SkillName = "move_group";
    private const string PathSeparator = " > ";

    private readonly IGroupRepository _groupRepository;
    private readonly IGroupScopeGuard _groupScopeGuard;
    private readonly IUnitOfWork _unitOfWork;

    public MoveGroupSkill(
        IGroupRepository groupRepository,
        IGroupScopeGuard groupScopeGuard,
        IUnitOfWork unitOfWork)
    {
        _groupRepository = groupRepository;
        _groupScopeGuard = groupScopeGuard;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var scope = await _groupScopeGuard.GetAccessAsync(context, cancellationToken);
        var groups = scope.Filter(await _groupRepository.List());

        var (group, groupError) = await ResolveAsync(parameters, "groupId", "groupName", groups);
        if (groupError != null)
        {
            return SkillResult.Error(groupError);
        }

        var (newParent, parentError) = await ResolveAsync(parameters, "newParentId", "newParentName", groups);
        if (parentError != null)
        {
            return SkillResult.Error(parentError);
        }

        if (!scope.IsInScope(group!))
        {
            return SkillResult.Error(scope.BuildOutOfScopeError(group!.Name));
        }

        if (!scope.IsInScope(newParent!))
        {
            return SkillResult.Error(scope.BuildOutOfScopeError(newParent!.Name));
        }

        if (group!.Id == newParent!.Id)
        {
            return SkillResult.Error($"Group '{group.Name}' cannot be moved under itself.");
        }

        if (group.Parent == newParent.Id)
        {
            return SkillResult.SuccessResult(
                new { GroupId = group.Id, NewParentId = newParent.Id },
                $"Group '{group.Name}' is already directly under '{newParent.Name}' — nothing to move.");
        }

        var parentPath = (await _groupRepository.GetPath(newParent.Id)).ToList();
        if (parentPath.Any(ancestor => ancestor.Id == group.Id))
        {
            return SkillResult.Error(
                $"Group '{newParent.Name}' is a descendant of '{group.Name}' — moving a group under its own " +
                "descendant would break the tree. Pick a parent outside the group's own subtree.");
        }

        try
        {
            await _unitOfWork.ExecuteInTransactionAsync(async () =>
            {
                await _groupRepository.MoveNode(group.Id, newParent.Id);
                await _unitOfWork.CompleteAsync();
                await ConfirmPersistedAsync(
                    SkillName,
                    () => _groupRepository.GetNoTracking(group.Id),
                    persisted => !persisted.IsDeleted && persisted.Parent == newParent.Id,
                    $"the move of group '{group.Name}' under '{newParent.Name}'");
                return true;
            });
        }
        catch (SkillVerificationException ex)
        {
            return SkillResult.Error(ex.Message);
        }

        var newPath = (await _groupRepository.GetPath(group.Id)).ToList();
        var pathDisplay = string.Join(PathSeparator, newPath.Select(g => g.Name));

        return SkillResult.SuccessResult(
            new
            {
                GroupId = group.Id,
                GroupName = group.Name,
                NewParentId = newParent.Id,
                NewParentName = newParent.Name,
                NewPath = pathDisplay
            },
            $"Group '{group.Name}' was moved under '{newParent.Name}' and confirmed in the database (verified). " +
            $"New position: {pathDisplay}.");
    }

    private async Task<(Group? Group, string? Error)> ResolveAsync(
        Dictionary<string, object> parameters,
        string idParameter,
        string nameParameter,
        IReadOnlyList<Group> groupsInScope)
    {
        var idStr = GetParameter<string>(parameters, idParameter);
        if (!string.IsNullOrWhiteSpace(idStr))
        {
            if (!Guid.TryParse(idStr, out var id))
            {
                return (null, $"Invalid {idParameter} UUID: {idStr}");
            }

            var byId = await _groupRepository.Get(id);
            return byId == null
                ? (null, $"Group with ID '{id}' not found.")
                : (byId, null);
        }

        var name = GetParameter<string>(parameters, nameParameter);
        if (string.IsNullOrWhiteSpace(name))
        {
            return (null, $"Provide either {idParameter} or {nameParameter} to identify the group.");
        }

        return GroupResolver.Resolve(groupsInScope, name);
    }
}
