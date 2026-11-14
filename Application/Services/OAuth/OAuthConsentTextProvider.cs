// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Picks the language of the OAuth consent page: the first entry of the visitor's preference list that a core table or an
/// installed pack claims, otherwise the installation language, otherwise English. Every key is then resolved in that
/// language; a key the language lacks is taken from English so the page never shows a bare key.
/// </summary>
/// <param name="installationLanguageResolver">Reads DEFAULT_LANGUAGE of the installation</param>

using Klacks.Api.Application.Interfaces.OAuth;
using Klacks.Api.Domain.Common;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Settings;

namespace Klacks.Api.Application.Services.OAuth;

public sealed class OAuthConsentTextProvider : IOAuthConsentTextProvider
{
    private readonly IInstallationLanguageResolver _installationLanguageResolver;

    public OAuthConsentTextProvider(IInstallationLanguageResolver installationLanguageResolver)
    {
        _installationLanguageResolver = installationLanguageResolver;
    }

    public async Task<OAuthConsentTextSet> GetAsync(
        IReadOnlyList<string> preferredLanguages,
        CancellationToken cancellationToken = default)
    {
        var code = preferredLanguages
            .Select(OAuthConsentTexts.ResolveClaimedLanguage)
            .FirstOrDefault(claimed => claimed != null);

        if (code == null)
        {
            var installationLanguage = await _installationLanguageResolver.ResolveAsync(cancellationToken);
            code = OAuthConsentTexts.ResolveClaimedLanguage(installationLanguage) ?? LanguageConfig.DefaultLanguageFallback;
        }

        var texts = OAuthConsentTexts.RequiredKeys.ToDictionary(
            key => key,
            key => OAuthConsentTexts.TryGetText(key, code, out var text) && !string.IsNullOrWhiteSpace(text)
                ? text
                : OAuthConsentTexts.EnglishOf(key),
            StringComparer.Ordinal);

        return new OAuthConsentTextSet(code, LanguageTag.IsRightToLeft(code), texts);
    }
}
