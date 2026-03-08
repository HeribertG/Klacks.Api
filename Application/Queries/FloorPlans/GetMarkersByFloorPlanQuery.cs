// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.FloorPlans;

public record GetMarkersByFloorPlanQuery(Guid FloorPlanId) : IRequest<IEnumerable<FloorPlanWorkMarkerResource>>;
