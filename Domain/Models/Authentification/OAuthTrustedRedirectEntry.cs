// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Authentification;

/// <summary>
/// One fixed entry of the shipped trusted list.
/// </summary>
/// <param name="DisplayName">Name shown to users</param>
/// <param name="Host">Host of the redirect URI</param>
/// <param name="RedirectUri">Exact redirect URI, compared ordinally against the raw request value</param>
public sealed record OAuthTrustedRedirectEntry(string DisplayName, string Host, string RedirectUri);
