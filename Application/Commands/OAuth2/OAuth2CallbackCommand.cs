// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Infrastructure.Mediator;
using Klacks.Api.Application.DTOs.OAuth2;
using Klacks.Api.Domain.DTOs.Registrations;
using Klacks.Api.Application.DTOs.Registrations;

namespace Klacks.Api.Application.Commands.OAuth2;

public record OAuth2CallbackCommand(OAuth2CallbackRequest Request) : IRequest<TokenResource>;
