// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Scoped policy that reads the OAuth connection settings and resolves the redirect URI of an authorization request.
/// On the public Playground access tokens issued through OAuth live 24 hours instead of 30 days; the nightly reset
/// of that database (drop and restore of a dump without OAuth rows) removes whatever remains.
/// </summary>
/// <param name="settingsReader">Reads the four setting rows in one query</param>
/// <param name="playgroundMode">Tells whether the instance is the public Playground</param>

using System.Text.Json;
using Klacks.Api.Application.Interfaces.OAuth;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Playground;
using Klacks.Api.Domain.Interfaces.Settings;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Application.Services.OAuth;

public sealed class OAuthRedirectTrustPolicy : IOAuthRedirectTrustPolicy
{
    private readonly ISettingsReader _settingsReader;
    private readonly IPlaygroundMode _playgroundMode;

    public OAuthRedirectTrustPolicy(ISettingsReader settingsReader, IPlaygroundMode playgroundMode)
    {
        _settingsReader = settingsReader;
        _playgroundMode = playgroundMode;
    }

    public TimeSpan AccessTokenLifetime => _playgroundMode.IsActive
        ? TimeSpan.FromHours(OAuthConstants.PlaygroundAccessTokenExpiresInHours)
        : TimeSpan.FromDays(OAuthConstants.AccessTokenExpiresInDays);

    public async Task<OAuthTrustSettings> GetSettingsAsync(CancellationToken cancellationToken = default)
    {
        var values = await _settingsReader.GetSettingsByTypesAsync(OAuthTrustSettings.Keys, cancellationToken);
        return OAuthTrustSettings.FromValues(values);
    }

    public async Task<OAuthRedirectResolution> ResolveAsync(
        OAuthClient client,
        string? requestedRedirectUri,
        CancellationToken cancellationToken = default)
    {
        var settings = await GetSettingsAsync(cancellationToken);
        var registeredUris = JsonSerializer.Deserialize<List<string>>(client.RedirectUrisJson) ?? [];

        string effective;
        if (string.IsNullOrEmpty(requestedRedirectUri))
        {
            if (registeredUris.Count != 1)
            {
                return OAuthRedirectResolution.Rejected(
                    settings,
                    OAuthConstants.ErrorInvalidRequest,
                    "The redirect_uri parameter is required.");
            }

            effective = registeredUris[0];
        }
        else
        {
            if (!registeredUris.Contains(requestedRedirectUri, StringComparer.Ordinal))
            {
                return OAuthRedirectResolution.Rejected(
                    settings,
                    OAuthConstants.ErrorInvalidRequest,
                    "The redirect_uri is not registered for this client.");
            }

            effective = requestedRedirectUri;
        }

        var decision = settings.Evaluate(effective);
        if (!decision.IsTrusted)
        {
            var (error, description) = OAuthTrustMessages.For(decision);
            return OAuthRedirectResolution.Rejected(settings, error, description);
        }

        return OAuthRedirectResolution.Resolved(effective, decision.Match!, settings);
    }
}
