// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.OAuth;

public record ValidateOAuthAuthorizeRequestQuery(
    string? ClientId,
    string? RedirectUri,
    string? ResponseType,
    string? CodeChallenge,
    string? CodeChallengeMethod) : IRequest<OAuthAuthorizeValidationResult>;
