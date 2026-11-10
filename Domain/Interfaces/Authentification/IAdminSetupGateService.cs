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

    /// <summary>
    /// True when a first-start request from this origin can be accepted at all: always for a request straight from the
    /// machine itself, otherwise only when the installer configured a one-time setup code.
    /// </summary>
    /// <param name="isLocalRequest">The request comes straight from the machine itself, without a proxy</param>
    bool IsFirstStartReachable(bool isLocalRequest);

    /// <summary>
    /// True for a request straight from the machine itself, otherwise only when the supplied code matches the configured
    /// one-time setup code (constant-time comparison); false whenever no code is configured.
    /// </summary>
    /// <param name="isLocalRequest">The request comes straight from the machine itself, without a proxy</param>
    /// <param name="setupCode">Code the visitor typed</param>
    bool IsFirstStartCodeAccepted(bool isLocalRequest, string? setupCode);

    /// <summary>
    /// True when a sign-in with this e-mail must be refused before the password is checked: the account is the seeded
    /// admin, the request does not come straight from the machine itself, a one-time setup code is configured and the
    /// first start is still open. Such an installation is claimed only through the first-start page with the code, so
    /// the public default password no longer opens a way around it; checking before the password also keeps Identity
    /// from rehashing the seeded hash, which would close the first start.
    /// </summary>
    /// <param name="email">E-mail address typed on the sign-in page</param>
    /// <param name="isLocalRequest">The request comes straight from the machine itself, without a proxy</param>
    Task<bool> IsSeedAdminSignInReservedForSetupCodeAsync(string email, bool isLocalRequest);
}
