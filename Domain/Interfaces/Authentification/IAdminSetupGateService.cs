// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Reports whether a freshly deployed instance still requires its own admin account before the
/// seeded "admin@test.com" account may keep being used for anything beyond that setup step.
/// </summary>
public interface IAdminSetupGateService
{
    /// <summary>
    /// True while the instance is not the Playground and the seeded admin account is not yet
    /// deactivated.
    /// </summary>
    Task<bool> IsGateActiveAsync();

    /// <summary>
    /// True when this environment (Development or Playground) never enforces the own-admin setup gate,
    /// regardless of whether the seeded admin account has already been deactivated.
    /// </summary>
    Task<bool> IsExemptAsync();

    /// <summary>
    /// True while a fresh installation may still be claimed without signing in: the gate is active, the seeded admin
    /// still holds the Admin role with its seeded password hash, and no other active account holds the Admin role.
    /// Anyone who reaches the instance could do the same by signing in with the public default password, so the
    /// first-start path opens no door the seeded account does not already open. Reads the database fresh on every
    /// call; callers that act on the answer re-check it under the administrator-role lock.
    /// </summary>
    Task<bool> IsFirstStartSetupOpenAsync();
}
