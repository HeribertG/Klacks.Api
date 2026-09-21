// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The one answer to "may this account release this skill": the account's current roles, expanded into
/// rights, must cover every permission the skill requires, and an Admin passes regardless - the same
/// rule SkillExecutorService.ValidatePermissions applies at execution time. Shared by the approval roster
/// (who is asked at all) and the delegation handler (who may say "you handle this one"), so the two
/// gates cannot drift apart and let somebody approve a remediation that is then refused for them.
/// </summary>

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillPermissionGate
{
    /// <summary>Whether this account's current rights cover <paramref name="requiredPermissions"/>.</summary>
    /// <param name="user">The account whose roles are read fresh - never a cached set.</param>
    /// <param name="requiredPermissions">SkillDescriptor.RequiredPermissions of the skill in question.</param>
    Task<bool> HoldsAsync(AppUser user, IReadOnlyCollection<string> requiredPermissions);

    /// <summary>Same check by user id; an unknown account holds nothing and answers false.</summary>
    Task<bool> HoldsAsync(string userId, IReadOnlyCollection<string> requiredPermissions);
}
