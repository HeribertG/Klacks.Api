// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Bots;

public record RevokeKlacksBotTokenCommand(Guid Id) : IRequest<bool>;
