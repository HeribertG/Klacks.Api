// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Constants;

public static class AdminSetupMessages
{
    public const string AlreadyCompleted = "Own admin account setup was already completed.";

    public const string AdminRoleNotGranted = "Own admin account setup could not grant the Admin role to the new account.";

    public const string SeedAdminNotDeactivated = "Own admin account setup could not deactivate the seeded admin account.";

    public const string NotRequiredInEnvironment = "Own admin account setup is not required in this environment (Development or Playground); the seeded admin account stays active.";
}
