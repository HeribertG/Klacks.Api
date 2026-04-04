// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to retrieve all children (sub-works, sub-breaks) of a container work.
/// </summary>
/// <param name="WorkId">The ID of the parent container work</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Works;

public record GetContainerWorkChildrenQuery(Guid WorkId) : IRequest<ContainerWorkChildrenResource>;
