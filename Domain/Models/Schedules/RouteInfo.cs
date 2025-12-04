namespace Klacks.Api.Domain.Models.Schedules;

public class RouteInfo
{
    public string StartBase { get; set; } = string.Empty;

    public string EndBase { get; set; } = string.Empty;

    public double TotalDistanceKm { get; set; }

    public string EstimatedTravelTime { get; set; } = string.Empty;

    public string TravelTimeFromStartBase { get; set; } = string.Empty;

    public double DistanceFromStartBaseKm { get; set; }

    public double DistanceToEndBaseKm { get; set; }

    public string TravelTimeToEndBase { get; set; } = string.Empty;

    public List<RouteLocation> OptimizedRoute { get; set; } = new List<RouteLocation>();

    public List<RouteSegmentDirections>? SegmentDirections { get; set; }
}
