// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Maps the role level a caller typed (Admin, Authorised or User, any casing) to the canonical role name. The one
/// place for the role levels the user-role skills accept.
/// </summary>
/// <param name="role">The role level as supplied</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.Services.Authentication;

public static class AssignableRoleParser
{
    private static readonly string[] AssignableRoles = [Roles.Admin, Roles.Authorised, Roles.User];

    public static string? Normalize(string? role)
    {
        if (string.IsNullOrWhiteSpace(role))
        {
            return null;
        }

        return AssignableRoles.FirstOrDefault(
            candidate => role.Equals(candidate, StringComparison.OrdinalIgnoreCase));
    }

    public static string InvalidRoleMessage(string? role) =>
        $"Invalid role '{role}'. Allowed values are: {string.Join(", ", AssignableRoles)}.";
}
