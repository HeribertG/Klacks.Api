// Copyright (c) Heribert Gasparoli Private. All rights reserved.

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
}
