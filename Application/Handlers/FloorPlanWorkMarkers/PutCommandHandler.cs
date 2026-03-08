// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlanWorkMarkers;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<FloorPlanWorkMarkerResource>, FloorPlanWorkMarkerResource?>
{
    private readonly IFloorPlanWorkMarkerRepository _markerRepository;
    private readonly FloorPlanMapper _floorPlanMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IFloorPlanWorkMarkerRepository markerRepository,
        FloorPlanMapper floorPlanMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _markerRepository = markerRepository;
        _floorPlanMapper = floorPlanMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanWorkMarkerResource?> Handle(PutCommand<FloorPlanWorkMarkerResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _markerRepository.Get(request.Resource.Id);

            if (existing == null)
            {
                throw new KeyNotFoundException($"FloorPlanWorkMarker with ID {request.Resource.Id} not found");
            }

            existing.FloorPlanId = request.Resource.FloorPlanId;
            existing.WorkId = request.Resource.WorkId;
            existing.ClientId = request.Resource.ClientId;
            existing.Label = request.Resource.Label;
            existing.X = request.Resource.X;
            existing.Y = request.Resource.Y;
            existing.Width = request.Resource.Width;
            existing.Height = request.Resource.Height;
            existing.Color = request.Resource.Color;
            existing.MarkerType = request.Resource.MarkerType;
            existing.UpdateTime = DateTime.UtcNow;

            await _markerRepository.Put(existing);
            await _unitOfWork.CompleteAsync();

            return _floorPlanMapper.ToMarkerResource(existing);
        }, "UpdateFloorPlanWorkMarker", new { request.Resource.Id });
    }
}
