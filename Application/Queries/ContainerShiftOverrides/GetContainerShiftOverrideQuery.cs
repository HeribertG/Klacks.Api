// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.ContainerShiftOverrides;

public record GetContainerShiftOverrideQuery(Guid ContainerId, DateOnly Date) : IRequest<ContainerShiftOverrideResource?>;
