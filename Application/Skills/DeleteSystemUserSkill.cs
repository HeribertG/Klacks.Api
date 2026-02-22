// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class DeleteSystemUserSkill : BaseSkill
{
    private readonly IUserManagementService _userManagementService;

    public override string Name => "delete_system_user";

    public override string Description =>
        "Deletes a system user account by user ID. " +
        "This permanently removes the login account. " +
        "Requires admin permissions.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanEditSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "userId",
            "The ID of the user to delete",
            SkillParameterType.String,
            Required: true)
    ];

    public DeleteSystemUserSkill(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
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

        var userGuid = Guid.Parse(userId);
        var (success, message) = await _userManagementService.DeleteUserAsync(userGuid);

        if (!success)
            return SkillResult.Error($"Failed to delete user: {message}");

        var resultData = new
        {
            UserId = userId,
            DeletedUserName = $"{user.FirstName} {user.LastName}",
            DeletedEmail = user.Email
        };

        return SkillResult.SuccessResult(resultData,
            $"User '{user.FirstName} {user.LastName}' ({user.Email}) was successfully deleted.");
    }
}
