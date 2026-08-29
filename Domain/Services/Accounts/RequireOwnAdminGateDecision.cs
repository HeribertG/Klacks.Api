// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure decision logic determining whether a deployed instance must still force the creation of its
/// own admin account before the seeded "admin@test.com" account may keep being used.
/// </summary>
/// <param name="isExempt">True on the public demo/Playground instance or a local dev environment</param>
/// <param name="seedAdminStillActive">True while the seeded admin account has not been deactivated</param>

namespace Klacks.Api.Domain.Services.Accounts;

public static class RequireOwnAdminGateDecision
{
    public static bool Decide(bool isExempt, bool seedAdminStillActive)
    {
        return !isExempt && seedAdminStillActive;
    }
}
