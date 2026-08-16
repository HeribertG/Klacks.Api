// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Assistant;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Assistant;

public record EvaluateAutonomyLevelChangeQuery(Guid UserId, AutonomyLevel TargetLevel)
    : IRequest<AutonomyLevelChangeEvaluationResult>;
