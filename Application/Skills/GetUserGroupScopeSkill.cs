// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("get_user_group_scope")]
public class GetUserGroupScopeSkill : BaseSkillImplementation
{
    private readonly IUserManagementService _userManagementService;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;

    public GetUserGroupScopeSkill(
        IUserManagementService userManagementService,
        IGroupRepository groupRepository,
        IGroupVisibilityRepository groupVisibilityRepository)
    {
        _userManagementService = userManagementService;
        _groupRepository = groupRepository;
        _groupVisibilityRepository = groupVisibilityRepository;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredString(parameters, "userId");

        var user = await _userManagementService.FindUserByIdAsync(userId);
        if (user == null)
            return SkillResult.Error($"User with ID '{userId}' not found.");

        var userName = $"{user.FirstName} {user.LastName}";
        var isAdmin = await _userManagementService.IsUserInRoleAsync(user, Roles.Admin);
        var roots = (await _groupRepository.GetRoots()).ToList();

        if (isAdmin)
        {
            var allGroups = roots.Select(g => new { g.Id, g.Name }).ToList();
            return SkillResult.SuccessResult(
                new { UserId = userId, UserName = userName, IsAdmin = true, AssignedGroups = allGroups },
                $"'{userName}' is an Admin and therefore sees all {allGroups.Count} groups by default; group scope cannot be restricted for admins.");
        }

        var existingVisibilities = (await _groupVisibilityRepository.GetGroupVisibilityList()).ToList();
        var assignedGroupIds = existingVisibilities
            .Where(gv => gv.AppUserId == userId)
            .Select(gv => gv.GroupId)
            .ToHashSet();

        var assignedGroups = roots
            .Where(g => assignedGroupIds.Contains(g.Id))
            .Select(g => new { g.Id, g.Name })
            .ToList();

        var message = assignedGroups.Count == 0
            ? $"'{userName}' currently has no group scope configured — this account cannot see any group's data until set_user_group_scope is used."
            : $"'{userName}' currently sees: {string.Join(", ", assignedGroups.Select(g => g.Name))}.";

        return SkillResult.SuccessResult(
            new { UserId = userId, UserName = userName, IsAdmin = false, AssignedGroups = assignedGroups },
            message);
    }
}
