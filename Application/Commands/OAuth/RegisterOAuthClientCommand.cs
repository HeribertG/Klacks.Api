// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.OAuth;

public record RegisterOAuthClientCommand(OAuthClientRegistrationRequest Request) : IRequest<OAuthClientRegistrationResult>;
