using Klacks.Api.Domain.Services.RouteOptimization;
using Microsoft.AspNetCore.Mvc;

namespace Klacks.Api.Presentation.Controllers.v1.UserBackend;

[ApiController]
[Route("api/v1/backend/[controller]")]
public class RouteOptimizationController : ControllerBase
{
    private readonly IRouteOptimizationService _routeOptimizationService;
    private readonly ILogger<RouteOptimizationController> _logger;

    public RouteOptimizationController(
        IRouteOptimizationService routeOptimizationService,
        ILogger<RouteOptimizationController> logger)
    {
        _routeOptimizationService = routeOptimizationService;
        _logger = logger;
    }

    [HttpGet("distance-matrix")]
    public async Task<ActionResult<DistanceMatrixResponse>> GetDistanceMatrix(
        [FromQuery] Guid containerId,
        [FromQuery] int weekday,
        [FromQuery] bool isHoliday = false)
    {
        _logger.LogInformation(
            "Getting distance matrix for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}",
            containerId, weekday, isHoliday);

        try
        {
            var result = await _routeOptimizationService.CalculateDistanceMatrixAsync(
                containerId, weekday, isHoliday);

            var response = new DistanceMatrixResponse
            {
                Locations = result.Locations.Select(l => new LocationDto
                {
                    Name = l.Name,
                    Address = l.Address,
                    Latitude = l.Latitude,
                    Longitude = l.Longitude,
                    ShiftId = l.ShiftId
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

    [HttpGet("optimize-route")]
    public async Task<ActionResult<RouteOptimizationResponse>> OptimizeRoute(
        [FromQuery] Guid containerId,
        [FromQuery] int weekday,
        [FromQuery] bool isHoliday = false,
        [FromQuery] string? startBase = null,
        [FromQuery] string? endBase = null)
    {
        _logger.LogInformation(
            "Optimizing route for Container: {ContainerId}, Weekday: {Weekday}, IsHoliday: {IsHoliday}, StartBase: {StartBase}, EndBase: {EndBase}",
            containerId, weekday, isHoliday, startBase, endBase);

        try
        {
            var result = await _routeOptimizationService.OptimizeRouteAsync(
                containerId, weekday, isHoliday, startBase, endBase);

            var routeSteps = new List<RouteStepDto>();

            for (int i = 0; i < result.OptimizedRoute.Count; i++)
            {
                var location = result.OptimizedRoute[i];
                double distanceToNext = 0;
                TimeSpan travelTimeToNext = TimeSpan.Zero;

                if (i < result.OptimizedRoute.Count - 1 && result.RouteIndices.Count > i + 1)
                {
                    var currentIndex = result.RouteIndices[i];
                    var nextIndex = result.RouteIndices[i + 1];
                    distanceToNext = result.DistanceMatrix[currentIndex, nextIndex];
                    travelTimeToNext = EstimateTravelTime(distanceToNext);
                }

                routeSteps.Add(new RouteStepDto
                {
                    Order = i + 1,
                    Name = location.Name,
                    Address = location.Address,
                    Latitude = location.Latitude,
                    Longitude = location.Longitude,
                    ShiftId = location.ShiftId,
                    DistanceToNextKm = distanceToNext,
                    TravelTimeToNext = travelTimeToNext
                });
            }

            var response = new RouteOptimizationResponse
            {
                OptimizedRoute = routeSteps,
                TotalDistanceKm = result.TotalDistanceKm,
                EstimatedTravelTime = result.EstimatedTravelTime,
                TravelTimeFromStartBase = result.TravelTimeFromStartBase,
                DistanceFromStartBaseKm = result.DistanceFromStartBaseKm,
                DistanceToEndBaseKm = result.DistanceToEndBaseKm,
                TravelTimeToEndBase = result.TravelTimeToEndBase
            };

            return Ok(response);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error optimizing route");
            return StatusCode(500, "Error optimizing route");
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

    private TimeSpan EstimateTravelTime(double distanceKm)
    {
        const double AVERAGE_SPEED_KMH = 50.0;
        var hours = distanceKm / AVERAGE_SPEED_KMH;
        return TimeSpan.FromHours(hours);
    }
}

public class DistanceMatrixResponse
{
    public List<LocationDto> Locations { get; set; } = new();
    public double[][] Matrix { get; set; } = Array.Empty<double[]>();
}

public class RouteOptimizationResponse
{
    public List<RouteStepDto> OptimizedRoute { get; set; } = new();
    public double TotalDistanceKm { get; set; }
    public TimeSpan EstimatedTravelTime { get; set; }
    public TimeSpan TravelTimeFromStartBase { get; set; }
    public double DistanceFromStartBaseKm { get; set; }
    public double DistanceToEndBaseKm { get; set; }
    public TimeSpan TravelTimeToEndBase { get; set; }
}

public class LocationDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid ShiftId { get; set; }
}

public class RouteStepDto : LocationDto
{
    public int Order { get; set; }
    public double DistanceToNextKm { get; set; }
    public TimeSpan TravelTimeToNext { get; set; }
}
