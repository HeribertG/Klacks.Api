// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Authentification;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Authentification;

public record CreatePersonalAccessTokenCommand(string UserId, string Name, int? ExpiresInDays) : IRequest<PersonalAccessTokenCreatedDto>;
