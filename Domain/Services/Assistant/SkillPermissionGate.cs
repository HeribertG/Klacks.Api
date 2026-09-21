// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Default <see cref="ISkillPermissionGate"/>: reads the account's roles through IUserManagementService,
/// expands them with Permissions.ExpandRoles and checks the skill's required permissions against the
/// result, with the Admin bypass. Roles are read on every call on purpose - a role revoked a minute ago
/// must already count here.
/// </summary>
/// <param name="userManagementService">Account lookup and the account's current roles.</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Interfaces.Authentification;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Services.Assistant;

public sealed class SkillPermissionGate : ISkillPermissionGate
{
    private readonly IUserManagementService _userManagementService;

    public SkillPermissionGate(IUserManagementService userManagementService)
    {
        _userManagementService = userManagementService;
    }

    public async Task<bool> HoldsAsync(AppUser user, IReadOnlyCollection<string> requiredPermissions)
    {
        var roles = await _userManagementService.GetUserRolesAsync(user);
        var rights = Permissions.ExpandRoles(roles);

        if (rights.Contains(Roles.Admin))
        {
            return true;
        }

        return requiredPermissions.All(rights.Contains);
    }

    public async Task<bool> HoldsAsync(string userId, IReadOnlyCollection<string> requiredPermissions)
    {
        var user = await _userManagementService.FindUserByIdAsync(userId);
        return user is not null && await HoldsAsync(user, requiredPermissions);
    }
}
