// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.Assistant;

public record SetAutonomyLevelCommand(Guid UserId, AutonomyLevel Level) : IRequest<AutonomyLevel>;
