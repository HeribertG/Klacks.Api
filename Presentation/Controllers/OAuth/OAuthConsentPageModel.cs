// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Services.OAuth;
using Klacks.Api.Domain.Models.Authentification;

namespace Klacks.Api.Presentation.Controllers.OAuth;

/// <summary>
/// Everything the consent page shows. Name, host and class come from the trusted-list entry, never from the client.
/// </summary>
/// <param name="FormAction">URL the consent form posts to</param>
/// <param name="Match">Trusted-list entry of the redirect target</param>
/// <param name="IsWriteAllowed">Whether the read-and-change option is offered</param>
/// <param name="AccessTokenLifetime">How long the resulting access token lives</param>
/// <param name="Texts">The sentences in the language chosen for the visitor</param>
/// <param name="HiddenFields">OAuth request parameters and the anti-forgery token carried through the form</param>
/// <param name="ErrorMessage">Already translated error line, or null</param>
public sealed record OAuthConsentPageModel(
    string FormAction,
    OAuthRedirectMatch Match,
    bool IsWriteAllowed,
    TimeSpan AccessTokenLifetime,
    OAuthConsentTextSet Texts,
    IReadOnlyDictionary<string, string> HiddenFields,
    string? ErrorMessage);
