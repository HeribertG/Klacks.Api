// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.Commands;
using Klacks.Api.Application.Interfaces;
using Klacks.Api.Application.Mappers;
using Klacks.Api.Application.DTOs.FloorPlans;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.FloorPlans;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Handlers.FloorPlans;

public class PutCommandHandler : BaseHandler, IRequestHandler<PutCommand<FloorPlanResource>, FloorPlanResource?>
{
    private readonly IFloorPlanRepository _floorPlanRepository;
    private readonly IFloorPlanWorkMarkerRepository _markerRepository;
    private readonly FloorPlanMapper _floorPlanMapper;
    private readonly IUnitOfWork _unitOfWork;

    public PutCommandHandler(
        IFloorPlanRepository floorPlanRepository,
        IFloorPlanWorkMarkerRepository markerRepository,
        FloorPlanMapper floorPlanMapper,
        IUnitOfWork unitOfWork,
        ILogger<PutCommandHandler> logger)
        : base(logger)
    {
        _floorPlanRepository = floorPlanRepository;
        _markerRepository = markerRepository;
        _floorPlanMapper = floorPlanMapper;
        _unitOfWork = unitOfWork;
    }

    public async Task<FloorPlanResource?> Handle(PutCommand<FloorPlanResource> request, CancellationToken cancellationToken)
    {
        return await ExecuteAsync(async () =>
        {
            var existing = await _floorPlanRepository.Get(request.Resource.Id);

            if (existing == null)
            {
                throw new KeyNotFoundException($"FloorPlan with ID {request.Resource.Id} not found");
            }

            existing.Name = request.Resource.Name;
            existing.Description = request.Resource.Description;
            existing.CanvasJson = request.Resource.CanvasJson;
            existing.ThumbnailData = request.Resource.ThumbnailData;
            existing.UpdateTime = DateTime.UtcNow;

            await SyncWorkMarkersAsync(existing, request.Resource.WorkMarkers);

            await _floorPlanRepository.Put(existing);
            await _unitOfWork.CompleteAsync();

            return _floorPlanMapper.ToFloorPlanResource(existing);
        }, "UpdateFloorPlan", new { request.Resource.Id });
    }

    private async Task SyncWorkMarkersAsync(FloorPlan floorPlan, List<FloorPlanWorkMarkerResource>? markers)
    {
        if (markers == null)
        {
            return;
        }

        var existingMarkers = await _markerRepository.GetByFloorPlanIdAsync(floorPlan.Id);
        var incomingIds = markers.Where(m => m.Id != Guid.Empty).Select(m => m.Id).ToHashSet();

        foreach (var existingMarker in existingMarkers)
        {
            if (!incomingIds.Contains(existingMarker.Id))
            {
                await _markerRepository.Delete(existingMarker.Id);
            }
        }

        foreach (var markerResource in markers)
        {
            if (markerResource.Id == Guid.Empty)
            {
                var newMarker = _floorPlanMapper.ToMarkerEntity(markerResource);
                newMarker.Id = Guid.NewGuid();
                newMarker.FloorPlanId = floorPlan.Id;
                newMarker.CreateTime = DateTime.UtcNow;
                await _markerRepository.Add(newMarker);
            }
            else
            {
                var existingMarker = existingMarkers.FirstOrDefault(m => m.Id == markerResource.Id);
                if (existingMarker != null)
                {
                    existingMarker.X = markerResource.X;
                    existingMarker.Y = markerResource.Y;
                    existingMarker.Width = markerResource.Width;
                    existingMarker.Height = markerResource.Height;
                    existingMarker.Color = markerResource.Color;
                    existingMarker.Label = markerResource.Label;
                    existingMarker.MarkerType = markerResource.MarkerType;
                    existingMarker.WorkId = markerResource.WorkId;
                    existingMarker.ClientId = markerResource.ClientId;
                    existingMarker.UpdateTime = DateTime.UtcNow;
                    await _markerRepository.Put(existingMarker);
                }
            }
        }
    }
}
