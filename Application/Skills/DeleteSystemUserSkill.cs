// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

/// <summary>
/// Deletion is presented to the chat user exactly as it is in the admin UI - same "deleted" wording,
/// same finality - but the actual call is the reversible DeactivateUserAsync, not a hard delete (Owner
/// decision, 17.08.: hard delete was removed system-wide).
/// </summary>
[SkillImplementation("delete_system_user")]
public class DeleteSystemUserSkill : BaseSkillImplementation
{
    private readonly IUserManagementService _userManagementService;

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
        var (success, message) = await _userManagementService.DeactivateUserAsync(userGuid, context.UserId.ToString());

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
