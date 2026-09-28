// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.OAuth;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.OAuth;

public record RegisterOAuthClientCommand(OAuthClientRegistrationRequest Request) : IRequest<OAuthClientRegistrationResult>;
