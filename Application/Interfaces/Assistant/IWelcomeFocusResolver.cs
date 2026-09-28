// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

public interface IWelcomeFocusResolver
{
    /// <summary>
    /// The single most urgent open state for this user, or null when there is none, the user is
    /// not a planner, or resolving it failed. Never throws - the welcome must not fail on its
    /// state question.
    /// </summary>
    Task<WelcomeFocusResource?> ResolveAsync(string userId, CancellationToken cancellationToken = default);
}
