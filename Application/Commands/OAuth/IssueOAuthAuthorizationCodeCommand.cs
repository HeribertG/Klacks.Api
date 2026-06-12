// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.OAuth;

public record IssueOAuthAuthorizationCodeCommand(
    string Email,
    string Password,
    string ClientId,
    string RedirectUri,
    string CodeChallenge,
    string? Scope) : IRequest<OAuthAuthorizationCodeResult>;
