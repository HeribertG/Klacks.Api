// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolves one system user account from an id (preferred) or from a name, e-mail or login fragment. The one place
/// for the matching rule shared by the skills that act on a single account.
/// </summary>
/// <param name="users">User management service that lists and loads the accounts</param>
/// <param name="userId">Optional account id; wins over the name when given</param>
/// <param name="userName">Optional name, e-mail or login fragment used when no id is given</param>

using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Domain.Interfaces.Authentification;

namespace Klacks.Api.Application.Services.Authentication;

public static class SystemUserResolver
{
    public static async Task<SystemUserResolution> ResolveAsync(
        IUserManagementService users, string? userId, string? userName)
    {
        if (!string.IsNullOrWhiteSpace(userId))
        {
            var byId = await users.FindUserByIdAsync(userId);
            return byId == null
                ? Failure(SystemUserResolutionStatus.NotFoundById, userId)
                : new SystemUserResolution(SystemUserResolutionStatus.Resolved, byId, [], userId);
        }

        if (string.IsNullOrWhiteSpace(userName))
        {
            return Failure(SystemUserResolutionStatus.MissingInput, string.Empty);
        }

        var matches = (await users.GetUserListAsync())
            .Where(user => Matches(user, userName))
            .ToList();

        if (matches.Count == 0)
        {
            return Failure(SystemUserResolutionStatus.NotFoundByName, userName);
        }

        if (matches.Count > 1)
        {
            return new SystemUserResolution(SystemUserResolutionStatus.Ambiguous, null, matches, userName);
        }

        var resolved = await users.FindUserByIdAsync(matches[0].Id);
        return resolved == null
            ? Failure(SystemUserResolutionStatus.LoadFailed, userName)
            : new SystemUserResolution(SystemUserResolutionStatus.Resolved, resolved, matches, userName);
    }

    private static SystemUserResolution Failure(SystemUserResolutionStatus status, string input) =>
        new(status, null, [], input);

    private static bool Matches(UserResource user, string term) =>
        user.FirstName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        user.LastName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        user.Email.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        user.UserName.Contains(term, StringComparison.OrdinalIgnoreCase) ||
        $"{user.FirstName} {user.LastName}".Contains(term, StringComparison.OrdinalIgnoreCase);
}
