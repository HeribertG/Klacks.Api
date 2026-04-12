// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ContainerShiftOverrides;

public record GetContainerShiftOverridesForRangeQuery(Guid ContainerId, DateOnly FromDate, DateOnly ToDate) : IRequest<List<ContainerShiftOverrideResource>>;
