// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.FloorPlans;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlanWorkMarkers;

public class GetListByFloorPlanQueryHandler : BaseHandler, IRequestHandler<GetMarkersByFloorPlanQuery, IEnumerable<FloorPlanWorkMarkerResource>>
{
    private readonly IFloorPlanWorkMarkerRepository _markerRepository;
    private readonly FloorPlanMapper _floorPlanMapper;

    public GetListByFloorPlanQueryHandler(
        IFloorPlanWorkMarkerRepository markerRepository,
        FloorPlanMapper floorPlanMapper,
        ILogger<GetListByFloorPlanQueryHandler> logger)
        : base(logger)
    {
        _markerRepository = markerRepository;
        _floorPlanMapper = floorPlanMapper;
    }

    public async Task<IEnumerable<FloorPlanWorkMarkerResource>> Handle(GetMarkersByFloorPlanQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = await _markerRepository.GetByFloorPlanIdAsync(request.FloorPlanId);
            return entities.Select(_floorPlanMapper.ToMarkerResource);
        }, "GetMarkersByFloorPlan", new { request.FloorPlanId });
    }
}
