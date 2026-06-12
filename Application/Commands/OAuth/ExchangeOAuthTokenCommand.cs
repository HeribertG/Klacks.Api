// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.OAuth;

public record ExchangeOAuthTokenCommand(
    string? GrantType,
    string? Code,
    string? RedirectUri,
    string? ClientId,
    string? CodeVerifier) : IRequest<OAuthTokenResult>;
