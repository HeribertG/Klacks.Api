// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Assistant;

namespace Klacks.Api.Application.Interfaces.Assistant;

/// <summary>
/// Reads and advances the per-account Klacksy intro tour. Every signed-in user may read and write only their own
/// state; no admin right is involved.
/// </summary>
public interface IIntroTourStateService
{
    /// <summary>
    /// The intro slice for the welcome payload, or null when the caller has no account id. Never writes: an account
    /// without a row is reported as "pending" with ShouldOffer=true.
    /// </summary>
    /// <param name="userId">AspNetUsers id of the caller</param>
    Task<OnboardingResource?> GetStateAsync(string userId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Applies a reported status or event and/or a finished station to the caller's state and returns the new slice.
    /// Throws InvalidRequestException for an unknown status, a malformed station id or a missing account id.
    /// </summary>
    /// <param name="userId">AspNetUsers id of the caller</param>
    /// <param name="status">"offered", "snoozed", "in_progress", "completed", "dismissed", or null</param>
    /// <param name="completedStation">Intro station id to mark finished, or null</param>
    Task<OnboardingResource> ApplyAsync(string userId, string? status, string? completedStation, CancellationToken cancellationToken = default);

    /// <summary>
    /// Marks the caller's intro completed after the admin finished or dismissed the setup tour, so the same admin is
    /// not offered a second tour. A dismissed or completed intro stays as it is.
    /// </summary>
    /// <param name="userId">AspNetUsers id of the admin</param>
    Task CloseAfterSetupAsync(string userId, CancellationToken cancellationToken = default);
}
