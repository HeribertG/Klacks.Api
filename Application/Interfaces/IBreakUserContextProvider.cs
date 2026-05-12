// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Provides authenticated user context (roles, claims) for break lock-level operations.
/// Abstracts IHttpContextAccessor out of break command handlers.
/// </summary>
public interface IBreakUserContextProvider
{
    BreakUserContext GetUserContext();
}

/// <summary>
/// Resolved user context passed to IWorkLockLevelService for Seal/Unseal operations.
/// </summary>
/// <param name="IsAdmin">True when the current user has the Admin role.</param>
/// <param name="IsAuthorised">True when the current user carries the IsAuthorised claim.</param>
/// <param name="UserName">NameIdentifier claim value of the current user.</param>
public sealed record BreakUserContext(bool IsAdmin, bool IsAuthorised, string UserName);
