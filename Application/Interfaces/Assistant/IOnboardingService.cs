// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Reads and advances the Klacksy first-run setup tour state. Computes whether the tour should be
/// offered (fresh install + a live LLM + admin) and persists the user's progress and choices.
/// </summary>
public interface IOnboardingService
{
    /// <summary>
    /// Returns the current onboarding slice for the welcome payload, or null when onboarding is not
    /// relevant for this user (no fresh-install marker, or the user is not an admin).
    /// </summary>
    /// <param name="userRights">The caller's role/permission claims (admin gating)</param>
    Task<OnboardingResource?> GetStateAsync(IReadOnlyList<string> userRights, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies an optional new status and/or a newly completed station, persists the state, and
    /// returns the recomputed onboarding slice.
    /// </summary>
    /// <param name="status">New lifecycle status, or null to keep the current one</param>
    /// <param name="completedStation">Station id to mark completed, or null</param>
    /// <param name="userRights">The caller's role/permission claims (admin gating)</param>
    Task<OnboardingResource?> UpdateStateAsync(string? status, string? completedStation, IReadOnlyList<string> userRights, CancellationToken cancellationToken = default);
}
