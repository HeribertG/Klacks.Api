// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Server-side, single-use store for OAuth2 "state" values, protecting the authorization code
/// flow against login CSRF (an attacker injecting their own authorization code into a victim's session).
/// </summary>
public interface IOAuth2StateStore
{
    /// <summary>
    /// Generates a cryptographically random state value for the given provider, persists it with a
    /// short time-to-live, and returns it for use in the authorization redirect.
    /// </summary>
    /// <param name="providerId">Identity provider the state is issued for; embedded in the returned value so the callback can resolve the provider.</param>
    /// <param name="cancellationToken">Cancels the write of the issued state.</param>
    Task<string> CreateStateAsync(Guid providerId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Verifies that the given state was previously issued by <see cref="CreateStateAsync"/> and has not
    /// yet been consumed or expired, then removes it so it cannot be replayed.
    /// </summary>
    /// <param name="state">State value received on the OAuth2 callback.</param>
    /// <param name="cancellationToken">Cancels the consuming read and delete.</param>
    Task<bool> ValidateAndConsumeAsync(string state, CancellationToken cancellationToken = default);
}
