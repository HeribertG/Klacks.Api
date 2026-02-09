using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Skills;
using Klacks.Api.Domain.Services.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class ListSystemUsersSkill : BaseSkill
{
    private readonly IUserManagementService _userManagementService;

    public override string Name => "list_system_users";

    public override string Description =>
        "Lists all system user accounts (login accounts) in the user administration. " +
        "Returns user IDs, usernames, names, emails, and roles. " +
        "Use this to find user IDs for further operations like deletion or role changes.";

    public override SkillCategory Category => SkillCategory.Query;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanEditSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "searchTerm",
            "Optional search term to filter users by name or email",
            SkillParameterType.String,
            Required: false)
    ];

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
