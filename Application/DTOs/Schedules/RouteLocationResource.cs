namespace Klacks.Api.Application.DTOs.Schedules;

public class RouteLocationResource
{
    public string Name { get; set; } = string.Empty;

    public string Address { get; set; } = string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public string ShiftId { get; set; } = string.Empty;

    public double DistanceToNextKm { get; set; }

    public string TravelTimeToNext { get; set; } = string.Empty;

    public int Order { get; set; }
}
