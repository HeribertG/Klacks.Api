// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Application.DTOs.RouteOptimization;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Interfaces.RouteOptimization;
using Klacks.Api.Domain.Services.RouteOptimization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.UserBackend;

public class RouteOptimizationController : BaseController
{
    private readonly IRouteOptimizationService _routeOptimizationService;
    private readonly IContainerAutofillService _containerAutofillService;
    private readonly ILogger<RouteOptimizationController> _logger;

    public RouteOptimizationController(
        IRouteOptimizationService routeOptimizationService,
        IContainerAutofillService containerAutofillService,
        ILogger<RouteOptimizationController> logger)
    {
        _routeOptimizationService = routeOptimizationService;
        _containerAutofillService = containerAutofillService;
        _logger = logger;
    }

    [HttpGet("distance-matrix")]
    public async Task<ActionResult<DistanceMatrixResponse>> GetDistanceMatrix(
        [FromQuery] Guid containerId,
        [FromQuery] int weekday,
        [FromQuery] bool isHoliday = false,
        [FromQuery] ContainerTransportMode transportMode = ContainerTransportMode.ByCar)
    {
        _logger.LogInformation(
            "Getting distance matrix for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}, TransportMode: {TransportMode}",
            containerId, weekday, isHoliday, transportMode);

        try
        {
            var result = await _routeOptimizationService.CalculateDistanceMatrixAsync(
                containerId, weekday, isHoliday, transportMode);

            var response = new DistanceMatrixResponse
            {
                Locations = result.Locations.Select(l => new LocationDto
                {
                    Name = l.Name,
                    Address = l.Address,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    ShiftId = l.ShiftId,
                    TransportMode = l.TransportMode
                }).ToList(),
                Matrix = ConvertMatrixToJaggedArray(result.Matrix, result.Locations.Count)
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating distance matrix");
            return StatusCode(500, "Error calculating distance matrix");
        }
    }

    [HttpPost("optimize-route")]
    public async Task<ActionResult<RouteOptimizationResponse>> OptimizeRoute(
        [FromBody] List<Guid> shiftIds,
        [FromQuery] string? startBase = null,
        [FromQuery] string? endBase = null,
        [FromQuery] ContainerTransportMode transportMode = ContainerTransportMode.ByCar)
    {
        _logger.LogInformation(
            "Optimizing route for {Count} shifts, StartBase: {StartBase}, EndBase: {EndBase}, TransportMode: {TransportMode}",
            shiftIds.Count, startBase, endBase, transportMode);

        if (shiftIds.Count < 2)
        {
            return BadRequest("At least 2 shifts are required for route optimization");
        }

        try
        {
            var result = await _routeOptimizationService.OptimizeRouteByShiftIdsAsync(
                shiftIds, startBase, endBase, transportMode);

            var routeSteps = new List<RouteStepDto>();

            for (int i = 0; i < result.OptimizedRoute.Count; i++)
            {
                var location = result.OptimizedRoute[i];
                double distanceToNext = 0;
                TimeSpan travelTimeToNext = TimeSpan.Zero;

                if (i < result.OptimizedRoute.Count - 1 && i < result.FullRouteIndices.Count && i + 1 < result.FullRouteIndices.Count)
                {
                    var currentIndex = result.FullRouteIndices[i];
                    var nextIndex = result.FullRouteIndices[i + 1];
                    var nextLocation = result.OptimizedRoute[i + 1];

                    distanceToNext = result.DistanceMatrix[currentIndex, nextIndex];
                    travelTimeToNext = GetSegmentTravelTime(result, currentIndex, nextIndex, nextLocation.TransportMode);
                }

                routeSteps.Add(new RouteStepDto
                {
                    Order = i + 1,
                    Name = location.Name,
                    Address = location.Address,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    ShiftId = location.ShiftId,
                    TransportMode = location.TransportMode,
                    DistanceToNextKm = distanceToNext,
                    TravelTimeToNext = travelTimeToNext
                });
            }

            var segmentDirections = result.SegmentDirections?.Select(s => new RouteSegmentDirectionsDto
            {
                FromName = s.FromName,
                ToName = s.ToName,
                TransportMode = s.TransportMode,
                DistanceKm = s.DistanceKm,
                Duration = s.Duration,
                Steps = s.Steps.Select(step => new DirectionStepDto
                {
                    Instruction = step.Instruction,
                    StreetName = step.StreetName,
                    DistanceMeters = step.DistanceMeters,
                    DurationSeconds = step.DurationSeconds,
                    ManeuverType = step.ManeuverType
                }).ToList()
            }).ToList();

            var response = new RouteOptimizationResponse
            {
                OptimizedRoute = routeSteps,
                TotalDistanceKm = result.TotalDistanceKm,
                EstimatedTravelTime = result.EstimatedTravelTime,
                TravelTimeFromStartBase = result.TravelTimeFromStartBase,
                DistanceFromStartBaseKm = result.DistanceFromStartBaseKm,
                DistanceToEndBaseKm = result.DistanceToEndBaseKm,
                TravelTimeToEndBase = result.TravelTimeToEndBase,
                SegmentDirections = segmentDirections
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing route");
            return StatusCode(500, "Error optimizing route");
        }
    }

    [HttpGet("autofill")]
    public async Task<ActionResult<ContainerAutofillResponse>> Autofill(
        [FromQuery] Guid containerId,
        [FromQuery] int weekday,
        [FromQuery] bool isHoliday = false,
        [FromQuery] string? startBase = null,
        [FromQuery] string? endBase = null,
        [FromQuery] string? fromTime = null,
        [FromQuery] string? untilTime = null,
        [FromQuery] ContainerTransportMode transportMode = ContainerTransportMode.ByCar,
        [FromQuery] double timeRangeTolerance = 0.5,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrEmpty(startBase) || string.IsNullOrEmpty(endBase))
        {
            return BadRequest("Start base and end base addresses are required for autofill");
        }

        if (string.IsNullOrEmpty(fromTime) || string.IsNullOrEmpty(untilTime))
        {
            return BadRequest("fromTime and untilTime are required for autofill");
        }

        if (!TimeOnly.TryParse(fromTime, out var parsedFromTime) ||
            !TimeOnly.TryParse(untilTime, out var parsedUntilTime))
        {
            return BadRequest("Invalid time format. Use HH:mm format.");
        }

        _logger.LogInformation(
            "Autofill for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}, StartBase: {StartBase}, EndBase: {EndBase}, FromTime: {FromTime}, UntilTime: {UntilTime}",
            containerId, weekday, isHoliday, startBase, endBase, parsedFromTime, parsedUntilTime);

        try
        {
            var autofillRequest = new ContainerAutofillRequest(
                containerId, weekday, isHoliday, startBase, endBase,
                parsedFromTime, parsedUntilTime, transportMode, timeRangeTolerance,
                cancellationToken);

            var result = await _containerAutofillService.AutofillAsync(autofillRequest);

            var routeSteps = new List<RouteStepDto>();
            for (int i = 0; i < result.OptimizedRoute.Count; i++)
            {
                var location = result.OptimizedRoute[i];
                double distanceToNext = 0;
                TimeSpan travelTimeToNext = TimeSpan.Zero;

                if (i < result.OptimizedRoute.Count - 1 && i < result.FullRouteIndices.Count && i + 1 < result.FullRouteIndices.Count)
                {
                    var currentIndex = result.FullRouteIndices[i];
                    var nextIndex = result.FullRouteIndices[i + 1];
                    distanceToNext = result.DistanceMatrix[currentIndex, nextIndex];
                    travelTimeToNext = TimeSpan.FromSeconds(result.DurationMatrix[currentIndex, nextIndex]);
                }

                routeSteps.Add(new RouteStepDto
                {
                    Order = i + 1,
                    Name = location.Name,
                    Address = location.Address,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    ShiftId = location.ShiftId,
                    TransportMode = location.TransportMode,
                    DistanceToNextKm = distanceToNext,
                    TravelTimeToNext = travelTimeToNext
                });
            }

            var response = new ContainerAutofillResponse
            {
                OptimizedRoute = routeSteps,
                SelectedShiftIds = result.SelectedShiftIds,
                TotalDistanceKm = result.TotalDistanceKm,
                EstimatedTravelTime = result.EstimatedTravelTime,
                TotalWorkTime = result.TotalWorkTime,
                RemainingTime = result.RemainingTime,
                TotalAvailableShifts = result.TotalAvailableShifts,
                SelectedShiftCount = result.SelectedShiftCount,
                TravelTimeFromStartBase = result.TravelTimeFromStartBase,
                DistanceFromStartBaseKm = result.DistanceFromStartBaseKm,
                DistanceToEndBaseKm = result.DistanceToEndBaseKm,
                TravelTimeToEndBase = result.TravelTimeToEndBase,
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during container autofill");
            return StatusCode(500, "Error during container autofill");
        }
    }

    private double[][] ConvertMatrixToJaggedArray(double[,] matrix, int size)
    {
        var result = new double[size][];
        for (int i = 0; i < size; i++)
        {
            result[i] = new double[size];
            for (int j = 0; j < size; j++)
            {
                result[i][j] = matrix[i, j];
            }
        }
        return result;
    }

    private TimeSpan GetSegmentTravelTime(RouteOptimizationResult result, int fromIndex, int toIndex, TransportMode segmentTransportMode)
    {
        if (result.TransportMode != ContainerTransportMode.Mix || result.DurationMatricesByProfile == null)
        {
            return TimeSpan.FromSeconds(result.DurationMatrix[fromIndex, toIndex]);
        }

        var profile = segmentTransportMode switch
        {
            TransportMode.ByCar => "driving",
            TransportMode.ByBicycle => "cycling",
            TransportMode.ByFoot => "foot",
            _ => "driving"
        };

        if (result.DurationMatricesByProfile.TryGetValue(profile, out var durationMatrix))
        {
            return TimeSpan.FromSeconds(durationMatrix[fromIndex, toIndex]);
        }

        return TimeSpan.FromSeconds(result.DurationMatrix[fromIndex, toIndex]);
    }
}
