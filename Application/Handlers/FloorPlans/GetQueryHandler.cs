// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries.FloorPlans;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlans;

public class GetQueryHandler : BaseHandler, IRequestHandler<GetFloorPlanWithMarkersQuery, FloorPlanResource?>
{
    private readonly IFloorPlanRepository _floorPlanRepository;
    private readonly FloorPlanMapper _floorPlanMapper;

    public GetQueryHandler(
        IFloorPlanRepository floorPlanRepository,
        FloorPlanMapper floorPlanMapper,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _floorPlanRepository = floorPlanRepository;
        _floorPlanMapper = floorPlanMapper;
    }

    public async Task<FloorPlanResource?> Handle(GetFloorPlanWithMarkersQuery request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = await _floorPlanRepository.GetWithMarkersAsync(request.Id);

            if (entity == null)
            {
                throw new KeyNotFoundException($"FloorPlan with ID {request.Id} not found");
            }

            return _floorPlanMapper.ToFloorPlanResource(entity);
        }, "GetFloorPlan", new { request.Id });
    }
}
