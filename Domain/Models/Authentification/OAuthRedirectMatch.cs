// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Models.Authentification;

/// <summary>
/// A redirect URI that matched the trusted list.
/// </summary>
/// <param name="DisplayName">Name shown to users and written into the token name</param>
/// <param name="Host">Host shown prominently next to the name</param>
/// <param name="Class">Vendor, loopback or custom target</param>
/// <param name="Port">Port of a loopback target, otherwise null</param>
public sealed record OAuthRedirectMatch(string DisplayName, string Host, OAuthRedirectClass Class, int? Port);
