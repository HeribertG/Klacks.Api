// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.OAuth;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Interfaces.OAuth;

/// <summary>
/// Reads the OAuth connection settings and decides which redirect target an authorization request may use.
/// Scoped: it reads settings per call.
/// </summary>
public interface IOAuthRedirectTrustPolicy
{
    /// <summary>
    /// Lifetime of an access token issued through OAuth: 30 days, and 24 hours on the public Playground, whose database
    /// is reset every night.
    /// </summary>
    TimeSpan AccessTokenLifetime { get; }

    /// <summary>
    /// Loads the four OAuth settings in one read.
    /// </summary>
    /// <param name="cancellationToken">Cancels the read</param>
    Task<OAuthTrustSettings> GetSettingsAsync(CancellationToken cancellationToken = default);

    /// <summary>
    /// Picks the effective redirect URI of an authorization request (the requested one, or the only registered one when
    /// none was requested), checks that the client registered it and that the settings trust it.
    /// </summary>
    /// <param name="client">Registered client of the request</param>
    /// <param name="requestedRedirectUri">redirect_uri of the request, null or empty when absent</param>
    /// <param name="cancellationToken">Cancels the read</param>
    Task<OAuthRedirectResolution> ResolveAsync(OAuthClient client, string? requestedRedirectUri, CancellationToken cancellationToken = default);
}
