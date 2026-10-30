// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Advises on giving one system user account the role Admin, Authorised or User: which rights it would gain or
/// lose, whether settings access comes or goes, whether it already holds the role and whether it is the last active
/// administrator. Read-only advice computed in C# for the requested account only (no account list, no contact data);
/// applying the role is a separate step the system confirms once (the AdvisesFor target is Sensitive).
/// </summary>
/// <param name="role">Required. The role level to evaluate: Admin, Authorised or User.</param>
/// <param name="userId">Optional. The target user's id. Preferred over userName when known.</param>
/// <param name="userName">Optional. Name, e-mail or login used to resolve the target user when userId is omitted.</param>

using Klacks.Api.Application.DTOs.Authentification;
using Klacks.Api.Application.Services.Authentication;
using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation(SkillName)]
public class EvaluateUserRoleChangeSkill : BaseSkillImplementation
{
    public const string SkillName = "evaluate_user_role_change";

    private const string RoleParameter = "role";
    private const string UserIdParameter = "userId";
    private const string UserNameParameter = "userName";
    private const string UnnamedAccount = "The account";

    private readonly IUserManagementService _userManagementService;

    public EvaluateUserRoleChangeSkill(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var roleInput = GetParameter<string>(parameters, RoleParameter);
        var role = AssignableRoleParser.Normalize(roleInput);
        if (role == null)
        {
            return SkillResult.Error(AssignableRoleParser.InvalidRoleMessage(roleInput));
        }

        var resolution = await SystemUserResolver.ResolveAsync(
            _userManagementService,
            GetParameter<string>(parameters, UserIdParameter),
            GetParameter<string>(parameters, UserNameParameter));
        if (resolution.User == null)
        {
            return SkillResult.Error(resolution.ErrorMessage(listCandidates: false));
        }

        var user = resolution.User;
        var currentRoles = (await _userManagementService.GetUserRolesAsync(user)).ToList();
        var activeAdministratorCount = await _userManagementService.CountActiveAdministratorsAsync();
        var displayName = $"{user.FirstName} {user.LastName}".Trim();

        var evaluation = UserRoleChangeAdvisor.Evaluate(new UserRoleChangeFacts(
            user.Id,
            string.IsNullOrEmpty(displayName) ? UnnamedAccount : displayName,
            currentRoles,
            role,
            user.DeactivatedAt is null,
            activeAdministratorCount,
            Guid.TryParse(user.Id, out var accountId) && accountId == context.UserId));

        return SkillResult.SuccessResult(evaluation, evaluation.Recommendation);
    }
}
