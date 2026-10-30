// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Authentification;

/// <summary>
/// The deterministic findings the user role change advisor can raise.
/// </summary>
public enum UserRoleChangeFindingCode
{
    RoleUnchanged = 0,
    LastAdministratorDemotion = 1,
    OwnAccountLosesSettingsAccess = 2,
    AdminBypassLost = 3,
    AdminBypassGained = 4,
    SettingsAccessLost = 5,
    SettingsAccessGained = 6,
    RightsLost = 7,
    RightsGained = 8,
    AccountDeactivated = 9
}
