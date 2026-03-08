// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlanWorkMarkers;

public class PostCommandHandler : BaseHandler, IRequestHandler<PostCommand<FloorPlanWorkMarkerResource>, FloorPlanWorkMarkerResource?>
{
    private readonly IFloorPlanWorkMarkerRepository _markerRepository;
    private readonly FloorPlanMapper _floorPlanMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PostCommandHandler(
        IFloorPlanWorkMarkerRepository markerRepository,
        FloorPlanMapper floorPlanMapper,
        IUnitOfWork unitOfWork,
        ILogger<PostCommandHandler> logger)
        : base(logger)
    {
        _markerRepository = markerRepository;
        _floorPlanMapper = floorPlanMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanWorkMarkerResource?> Handle(PostCommand<FloorPlanWorkMarkerResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = _floorPlanMapper.ToMarkerEntity(request.Resource);
            entity.Id = Guid.NewGuid();
            entity.CreateTime = DateTime.UtcNow;

            await _markerRepository.Add(entity);
            await _unitOfWork.CompleteAsync();

            return _floorPlanMapper.ToMarkerResource(entity);
        }, "CreateFloorPlanWorkMarker", new { request.Resource.FloorPlanId });
    }
}
