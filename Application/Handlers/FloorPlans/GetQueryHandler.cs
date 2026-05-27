// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Handles retrieval of a single floor plan with its work markers, including name enrichment from related entities.
/// </summary>

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
    private readonly IFloorPlanMarkerDataLookup _markerDataLookup;

    public GetQueryHandler(
        IFloorPlanRepository floorPlanRepository,
        FloorPlanMapper floorPlanMapper,
        IFloorPlanMarkerDataLookup markerDataLookup,
        ILogger<GetQueryHandler> logger)
        : base(logger)
    {
        _floorPlanRepository = floorPlanRepository;
        _floorPlanMapper = floorPlanMapper;
        _markerDataLookup = markerDataLookup;
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

            var resource = _floorPlanMapper.ToFloorPlanResource(entity);
            await EnrichMarkerNamesAsync(resource, cancellationToken);
            return resource;
        }, "GetFloorPlan", new { request.Id });
    }

    private async Task EnrichMarkerNamesAsync(FloorPlanResource resource, CancellationToken cancellationToken)
    {
        if (resource.WorkMarkers == null || !resource.WorkMarkers.Any())
        {
            return;
        }

        var clientIds = resource.WorkMarkers
            .Where(m => m.ClientId.HasValue)
            .Select(m => m.ClientId!.Value)
            .Distinct()
            .ToList();

        var shiftIds = resource.WorkMarkers
            .Where(m => m.ShiftId.HasValue)
            .Select(m => m.ShiftId!.Value)
            .Distinct()
            .ToList();

        var clients = await _markerDataLookup.GetClientNamesAsync(clientIds);
        var shifts = await _markerDataLookup.GetShiftDetailsAsync(shiftIds);

        foreach (var marker in resource.WorkMarkers)
        {
            if (marker.ClientId.HasValue && clients.TryGetValue(marker.ClientId.Value, out var clientName))
            {
                marker.ClientName = clientName;
            }

            if (marker.ShiftId.HasValue && shifts.TryGetValue(marker.ShiftId.Value, out var shift))
            {
                marker.Abbreviation ??= shift.Abbreviation;
                marker.StartTime ??= shift.StartShift.ToString("HH:mm");
                marker.EndTime ??= shift.EndShift.ToString("HH:mm");
            }
        }
    }
}
