// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Authentification;

public record RevokePersonalAccessTokenCommand(Guid Id, string UserId) : IRequest<bool>;
