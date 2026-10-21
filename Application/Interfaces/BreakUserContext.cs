// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Resolved user context passed to IWorkLockLevelService for Seal/Unseal operations.
/// </summary>
/// <param name="IsAdmin">True when the current user has the Admin role.</param>
/// <param name="IsAuthorised">True when the current user has the Authorised role.</param>
/// <param name="UserName">NameIdentifier claim value of the current user.</param>

namespace Klacks.Api.Application.Interfaces;

public sealed record BreakUserContext(bool IsAdmin, bool IsAuthorised, string UserName);
