using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class LocationDto
{
    public string Name { get; set; } = string.Empty;
    public string Address { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public Guid ShiftId { get; set; }
    public TransportMode TransportMode { get; set; } = TransportMode.ByCar;
}
