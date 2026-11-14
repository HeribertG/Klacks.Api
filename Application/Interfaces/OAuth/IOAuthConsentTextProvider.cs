// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.OAuth;

namespace Klacks.Api.Application.Interfaces.OAuth;

/// <summary>
/// Chooses the language of the OAuth consent page and returns its sentences. Scoped: it reads the installation
/// language setting.
/// </summary>
public interface IOAuthConsentTextProvider
{
    /// <summary>
    /// The first language of the visitor's preference list that the catalogue knows, else the installation language
    /// (DEFAULT_LANGUAGE), else English. A sentence a language lacks is taken in English.
    /// </summary>
    /// <param name="preferredLanguages">Languages of the Accept-Language header, best first</param>
    /// <param name="cancellationToken">Cancels the settings read</param>
    Task<OAuthConsentTextSet> GetAsync(IReadOnlyList<string> preferredLanguages, CancellationToken cancellationToken = default);
}
