// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces;

public interface IUserService
{
    Guid? GetId();

    string? GetIdString();

    string GetUserName();

    /// <summary>
    /// Human readable name of the current principal, built from the GivenName and Surname claims
    /// that both the login token and the personal-access-token principal already carry. Costs no
    /// database round trip. Falls back to the Name claim and finally to the unknown-actor marker.
    /// </summary>
    string GetDisplayName();

    string? GetInstanceId();

    Task<bool> IsAdmin();
}
