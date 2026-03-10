// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Attributes;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

[SkillImplementation("list_system_users")]
public class ListSystemUsersSkill : BaseSkillImplementation
{
    private readonly IUserManagementService _userManagementService;

    public ListSystemUsersSkill(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var searchTerm = GetParameter<string>(parameters, "searchTerm");

        var users = await _userManagementService.GetUserListAsync();

        if (!string.IsNullOrEmpty(searchTerm))
        {
            users = users
                .Where(u =>
                    u.FirstName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.LastName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.Email.Contains(searchTerm, StringComparison.OrdinalIgnoreCase) ||
                    u.UserName.Contains(searchTerm, StringComparison.OrdinalIgnoreCase))
                .ToList();
        }

        var resultData = new
        {
            Users = users.Select(u => new
            {
                u.Id,
                u.UserName,
                u.FirstName,
                u.LastName,
                u.Email,
                u.IsAdmin,
                u.IsAuthorised
            }).ToList(),
            TotalCount = users.Count
        };

        var message = $"Found {users.Count} system user(s)" +
                      (!string.IsNullOrEmpty(searchTerm) ? $" matching '{searchTerm}'" : "") + ".";

        return SkillResult.SuccessResult(resultData, message);
    }
}
