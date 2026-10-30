// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The one rule for "this change would leave the installation without an active administrator", shared by the advisor
/// that warns about it, the skill that checks before writing and the user management service that enforces it on
/// every removal of the Admin role and on every deactivation of an administrator account. Pure and static so the
/// callers can never drift apart.
/// </summary>
/// <param name="holdsAdminRole">Whether the account holds the Admin role right now</param>
/// <param name="accountActive">False when the account is deactivated and therefore not counted as an active administrator</param>
/// <param name="activeAdministratorCount">Number of active accounts that currently hold the Admin role</param>

namespace Klacks.Api.Application.Services.Authentication;

public static class LastAdministratorRule
{
    public const string RefusalMessage =
        "The role was not changed: this account is the last active administrator and the installation must keep at least one. Make another account an administrator first.";

    public const string DeactivationRefusalMessage =
        "The account was not deactivated: it is the last active administrator and the installation must keep at least one. Make another account an administrator first.";

    private const int MinimumActiveAdministrators = 1;

    public static bool WouldRemoveLastActiveAdministrator(bool holdsAdminRole, bool accountActive, int activeAdministratorCount) =>
        holdsAdminRole && accountActive && activeAdministratorCount <= MinimumActiveAdministrators;
}
