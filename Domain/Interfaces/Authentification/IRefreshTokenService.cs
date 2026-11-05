// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Domain.Interfaces.Authentification;

/// <summary>
/// Domain service for refresh token management
/// </summary>
public interface IRefreshTokenService
{
    /// <summary>
    /// Issues a new refresh token for the user without removing the user's other
    /// tokens, so concurrent sessions (multiple tabs/devices) are preserved.
    /// Expired tokens are pruned and the count is capped.
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <returns>New refresh token</returns>
    Task<string> CreateRefreshTokenAsync(string userId);

    /// <summary>
    /// Renews a session without rotating its token: the presented token keeps its value and its expiry slides to
    /// now + the refresh token lifetime. One conditional UPDATE, so concurrent refreshes of the same session (tabs,
    /// interceptor, SignalR) all succeed. There is no absolute cap: the entity has no creation timestamp.
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <param name="refreshToken">Raw refresh token of the session</param>
    /// <returns>True when an unexpired token of this user was extended; false for an unknown, revoked or expired one</returns>
    Task<bool> ExtendRefreshTokenAsync(string userId, string refreshToken);

    /// <summary>
    /// Migrates a refresh token that the client held in local storage: issues a new token for the cookie and shortens the
    /// old one to RefreshTokenLifetime.LegacyTokenGrace (never lengthens it), so concurrent migrations with the same old
    /// token all succeed within the grace.
    /// </summary>
    /// <param name="userId">User's ID</param>
    /// <param name="legacyToken">Raw refresh token from the request body</param>
    /// <returns>The new refresh token, or null when the old one is unknown, expired or of another user</returns>
    Task<string?> ReplaceRefreshTokenAsync(string userId, string legacyToken);

    /// <summary>
    /// Gets user from refresh token
    /// </summary>
    /// <param name="refreshToken">Refresh token</param>
    /// <returns>User if token is valid</returns>
    Task<AppUser?> GetUserFromRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Removes all refresh tokens for a user
    /// </summary>
    /// <param name="userId">User's ID</param>
    Task RemoveAllUserRefreshTokensAsync(string userId);

    /// <summary>
    /// Removes the one refresh token presented, leaving the user's other sessions untouched. An unknown or empty
    /// token removes nothing.
    /// </summary>
    /// <param name="refreshToken">Raw refresh token as the client holds it</param>
    Task RemoveRefreshTokenAsync(string refreshToken);

    /// <summary>
    /// Calculates expiry time for access tokens
    /// </summary>
    /// <returns>Token expiry time</returns>
    DateTime CalculateTokenExpiryTime();
}