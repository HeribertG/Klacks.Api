// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Groups;

public record EvaluateGroupingByQualificationQuery(EntityTypeEnum EntityType)
    : IRequest<QualificationGroupCandidatesResult>;
