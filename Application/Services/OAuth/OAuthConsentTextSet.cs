// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.OAuth;

/// <summary>
/// The consent page sentences in the language chosen for one visitor.
/// </summary>
/// <param name="LanguageCode">Language code the sentences were taken from, written into the lang attribute</param>
/// <param name="IsRightToLeft">Whether the page has to be laid out right to left</param>
/// <param name="Texts">Every key of OAuthConsentTexts.RequiredKeys with its sentence</param>
public sealed record OAuthConsentTextSet(string LanguageCode, bool IsRightToLeft, IReadOnlyDictionary<string, string> Texts)
{
    public string Get(string key) => Texts[key];
}
