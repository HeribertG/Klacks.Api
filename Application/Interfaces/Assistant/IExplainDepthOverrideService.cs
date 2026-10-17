// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Writes or removes a user's own explanation-depth override, shared by the set_explain_depth skill and the
/// onboarding endpoint behind the "I already know Klacks" chip.
/// </summary>
public interface IExplainDepthOverrideService
{
    /// <summary>
    /// Stores "beginner" or "advanced" as the user's override (newest row wins, older rows are retired afterwards) or
    /// removes it for "auto", then drops the user's cached depth. Unknown values and an empty user write nothing.
    /// </summary>
    /// <param name="userId">The user whose override changes</param>
    /// <param name="depth">"beginner", "advanced" or "auto" (case and surrounding whitespace are ignored)</param>
    Task<ExplainDepthOverrideOutcome> SetAsync(Guid userId, string? depth, CancellationToken cancellationToken = default);
}
