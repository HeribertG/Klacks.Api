// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Bots;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Bots;

public record CreateKlacksBotTokenCommand(string Name, int? ExpiresInDays) : IRequest<KlacksBotTokenCreatedDto>;
