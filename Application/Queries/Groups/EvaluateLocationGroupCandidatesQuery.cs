// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Groups;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Groups;

public record EvaluateLocationGroupCandidatesQuery(EntityTypeEnum EntityType)
    : IRequest<LocationGroupCandidatesResult>;
