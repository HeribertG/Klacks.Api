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

    /// <summary>
    /// The granular rights of the current caller, expanded from the role claims of the request exactly
    /// as ClaimsPrincipalExtensions.GetUserRights does for the presentation layer — Permissions.ExpandRoles
    /// is the single expansion both use. Claim-based and free of a database round trip, so an application
    /// handler can ask the same rights question a controller asks. A request without a principal (or
    /// without role claims) yields the Planer floor, never an empty list.
    /// </summary>
    IReadOnlyList<string> GetRights();

    Task<bool> IsAdmin();
}
