namespace Klacks.Api.Application.DTOs.Schedules;

public class DirectionStepResource
{
    public string Instruction { get; set; } = string.Empty;

    public string StreetName { get; set; } = string.Empty;

    public double DistanceMeters { get; set; }

    public int DurationSeconds { get; set; }

    public string ManeuverType { get; set; } = string.Empty;
}
