using Klacks.Api.Application.Interfaces;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Associations;
using Klacks.Api.Domain.Models.Assistant;
using Klacks.Api.Domain.Services.Assistant.Skills.Implementations;

namespace Klacks.Api.Application.Skills;

public class SetUserGroupScopeSkill : BaseSkill
{
    private readonly IUserManagementService _userManagementService;
    private readonly IGroupRepository _groupRepository;
    private readonly IGroupVisibilityRepository _groupVisibilityRepository;
    private readonly IUnitOfWork _unitOfWork;

    public override string Name => "set_user_group_scope";

    public override string Description =>
        "Sets the group visibility scope for a user. " +
        "This controls which groups a user can see in the system. " +
        "Provide the user ID and one or more group names to assign. " +
        "Existing group scope entries for the user are replaced.";

    public override SkillCategory Category => SkillCategory.Crud;

    public override IReadOnlyList<string> RequiredPermissions => [Permissions.CanEditSettings];

    public override IReadOnlyList<SkillParameter> Parameters =>
    [
        new SkillParameter(
            "userId",
            "The ID of the user to set the group scope for",
            SkillParameterType.String,
            Required: true),
        new SkillParameter(
            "groupNames",
            "Comma-separated list of group names to assign (e.g. 'Deutschweiz Mitte, Romandie')",
            SkillParameterType.String,
            Required: true)
    ];

    public SetUserGroupScopeSkill(
        IUserManagementService userManagementService,
        IGroupRepository groupRepository,
        IGroupVisibilityRepository groupVisibilityRepository,
        IUnitOfWork unitOfWork)
    {
        _userManagementService = userManagementService;
        _groupRepository = groupRepository;
        _groupVisibilityRepository = groupVisibilityRepository;
        _unitOfWork = unitOfWork;
    }

    public override async Task<SkillResult> ExecuteAsync(
        SkillExecutionContext context,
        Dictionary<string, object> parameters,
        CancellationToken cancellationToken = default)
    {
        var userId = GetRequiredString(parameters, "userId");
        var groupNamesRaw = GetRequiredString(parameters, "groupNames");

        var user = await _userManagementService.FindUserByIdAsync(userId);
        if (user == null)
            return SkillResult.Error($"User with ID '{userId}' not found.");

        var isAdmin = await _userManagementService.IsUserInRoleAsync(user, Roles.Admin);
        if (isAdmin)
            return SkillResult.Error("Admin users have access to all groups by default. Group scope cannot be set for admins.");

        var requestedNames = groupNamesRaw
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .ToList();

        var roots = (await _groupRepository.GetRoots()).ToList();
        var matchedGroups = new List<Group>();
        var notFound = new List<string>();

        foreach (var name in requestedNames)
        {
            var match = roots.FirstOrDefault(g =>
                g.Name.Equals(name, StringComparison.OrdinalIgnoreCase));

            if (match != null)
                matchedGroups.Add(match);
            else
                notFound.Add(name);
        }

        if (notFound.Count > 0)
            return SkillResult.Error($"The following groups were not found as root groups: {string.Join(", ", notFound)}. " +
                                     $"Available root groups: {string.Join(", ", roots.Select(g => g.Name))}");

        var existingVisibilities = (await _groupVisibilityRepository.GetGroupVisibilityList()).ToList();
        var otherUserEntries = existingVisibilities.Where(gv => gv.AppUserId != userId).ToList();

        var newEntries = matchedGroups.Select(g => new GroupVisibility
        {
            AppUserId = userId,
            GroupId = g.Id
        }).ToList();

        var combined = otherUserEntries.Concat(newEntries).ToList();
        await _groupVisibilityRepository.SetGroupVisibilityList(combined);
        await _unitOfWork.CompleteAsync();

        var resultData = new
        {
            UserId = userId,
            UserName = $"{user.FirstName} {user.LastName}",
            AssignedGroups = matchedGroups.Select(g => new { g.Id, g.Name }).ToList()
        };

        return SkillResult.SuccessResult(resultData,
            $"Group scope for '{user.FirstName} {user.LastName}' set to: {string.Join(", ", matchedGroups.Select(g => g.Name))}");
    }
}
