// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

/// <summary>
/// Provides authenticated user context (roles, claims) for break lock-level operations.
/// Abstracts IHttpContextAccessor out of break command handlers.
/// </summary>
public interface IBreakUserContextProvider
{
    BreakUserContext GetUserContext();
}
