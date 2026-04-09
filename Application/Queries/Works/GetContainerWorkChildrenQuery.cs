// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query to retrieve all children (sub-works, sub-breaks) of a container work.
/// </summary>
/// <param name="WorkId">The ID of the parent container work</param>
/// <param name="IsHoliday">Whether the work date is a holiday — used to pick the matching ContainerTemplate row when filling in fallback bases / transport mode</param>
using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Works;

public record GetContainerWorkChildrenQuery(Guid WorkId, bool IsHoliday) : IRequest<ContainerWorkChildrenResource>;
