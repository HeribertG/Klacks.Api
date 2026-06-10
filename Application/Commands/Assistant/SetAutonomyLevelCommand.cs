// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public record SetAutonomyLevelCommand(Guid UserId, AutonomyLevel Level) : IRequest<AutonomyLevel>;
