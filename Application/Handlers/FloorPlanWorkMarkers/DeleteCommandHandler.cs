// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlanWorkMarkers;

public class DeleteCommandHandler : BaseHandler, IRequestHandler<DeleteCommand<FloorPlanWorkMarkerResource>, FloorPlanWorkMarkerResource?>
{
    private readonly IFloorPlanWorkMarkerRepository _markerRepository;
    private readonly FloorPlanMapper _floorPlanMapper;
    private readonly IUnitOfWork _unitOfWork;

    public DeleteCommandHandler(
        IFloorPlanWorkMarkerRepository markerRepository,
        FloorPlanMapper floorPlanMapper,
        IUnitOfWork unitOfWork,
        ILogger<DeleteCommandHandler> logger)
        : base(logger)
    {
        _markerRepository = markerRepository;
        _floorPlanMapper = floorPlanMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanWorkMarkerResource?> Handle(DeleteCommand<FloorPlanWorkMarkerResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var entity = await _markerRepository.Get(request.Id);

            if (entity == null)
            {
                throw new KeyNotFoundException($"FloorPlanWorkMarker with ID {request.Id} not found");
            }

            await _markerRepository.Delete(request.Id);
            await _unitOfWork.CompleteAsync();

            return _floorPlanMapper.ToMarkerResource(entity);
        }, "DeleteFloorPlanWorkMarker", new { request.Id });
    }
}
