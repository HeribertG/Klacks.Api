// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.Queries;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlans;

public class ListQueryHandler : BaseHandler, IRequestHandler<ListQuery<FloorPlanResource>, IEnumerable<FloorPlanResource>>
{
    private readonly IFloorPlanRepository _floorPlanRepository;
    private readonly FloorPlanMapper _floorPlanMapper;

    public ListQueryHandler(
        IFloorPlanRepository floorPlanRepository,
        FloorPlanMapper floorPlanMapper,
        ILogger<ListQueryHandler> logger)
        : base(logger)
    {
        _floorPlanRepository = floorPlanRepository;
        _floorPlanMapper = floorPlanMapper;
    }

    public async Task<IEnumerable<FloorPlanResource>> Handle(ListQuery<FloorPlanResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entities = await _floorPlanRepository.GetAllWithMarkersAsync();
            return entities.Select(fp =>
            {
                var resource = _floorPlanMapper.ToFloorPlanResource(fp);
                resource.CanvasJson = null;
                return resource;
            });
        }, "ListFloorPlans");
    }
}
