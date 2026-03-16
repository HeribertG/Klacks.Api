// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Response-DTO für den Container-Autofill-Endpoint.
/// Enthält die optimierte Route, ausgewählte Shifts und Zeitinformationen.
/// </summary>
/// <param name="OptimizedRoute">Optimierte Reihenfolge der Locations als RouteStepDto</param>
/// <param name="SelectedShiftIds">IDs der ausgewählten Shifts</param>
/// <param name="TotalDistanceKm">Gesamtdistanz in km</param>
/// <param name="EstimatedTravelTime">Geschätzte Gesamtzeit (Travel + OnSite)</param>
/// <param name="TotalWorkTime">Gesamte Vor-Ort-Zeit (Briefing + Work + Debriefing)</param>
/// <param name="RemainingTime">Verbleibende Zeit im Container-Budget</param>

namespace Klacks.Api.Application.DTOs.RouteOptimization;

public class ContainerAutofillResponse
{
    public List<RouteStepDto> OptimizedRoute { get; set; } = new();
    public List<Guid> SelectedShiftIds { get; set; } = new();
    public double TotalDistanceKm { get; set; }
    public TimeSpan EstimatedTravelTime { get; set; }
    public TimeSpan TotalWorkTime { get; set; }
    public TimeSpan RemainingTime { get; set; }
    public int TotalAvailableShifts { get; set; }
    public int SelectedShiftCount { get; set; }
    public TimeSpan TravelTimeFromStartBase { get; set; }
    public double DistanceFromStartBaseKm { get; set; }
    public double DistanceToEndBaseKm { get; set; }
    public TimeSpan TravelTimeToEndBase { get; set; }
    public List<RouteSegmentDirectionsDto>? SegmentDirections { get; set; }
}
