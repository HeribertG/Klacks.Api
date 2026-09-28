// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.OAuth;

public record ExchangeOAuthTokenCommand(
    string? GrantType,
    string? Code,
    string? RedirectUri,
    string? ClientId,
    string? CodeVerifier) : IRequest<OAuthTokenResult>;
