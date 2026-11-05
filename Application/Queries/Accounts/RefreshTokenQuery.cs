// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Registrations;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Accounts;

/// <summary>
/// Renews a session from a refresh token.
/// </summary>
/// <param name="RefreshRequest">Body of the request: access token and refresh token, both optional. A refresh token in the
/// body is the migration path of sessions that still hold it in the browser's local storage.</param>
/// <param name="CookieRefreshToken">Refresh token from the HttpOnly cookie, or null when the request carried none</param>
public record RefreshTokenQuery(RefreshRequestResource RefreshRequest, string? CookieRefreshToken = null) : IRequest<TokenResource?>;
