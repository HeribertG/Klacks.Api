// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Ergebnis des Container-Autofill: Enthält die optimierte Route sowie Informationen über ausgewählte Shifts.
/// </summary>
/// <param name="OptimizedRoute">Optimierte Reihenfolge der Locations</param>
/// <param name="SelectedShiftIds">IDs der ausgewählten Shifts</param>
/// <param name="TotalDistanceKm">Gesamtdistanz in km</param>
/// <param name="EstimatedTravelTime">Geschätzte Reisezeit</param>
/// <param name="TotalWorkTime">Gesamte Arbeitszeit (Briefing + Work + Debriefing)</param>
/// <param name="RemainingTime">Verbleibende Zeit im Budget</param>
/// <param name="TotalAvailableShifts">Anzahl verfügbarer Shifts</param>
/// <param name="SelectedShiftCount">Anzahl ausgewählter Shifts</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record ContainerAutofillResult(
    List<Location> OptimizedRoute,
    List<Guid> SelectedShiftIds,
    double TotalDistanceKm,
    TimeSpan EstimatedTravelTime,
    TimeSpan TotalWorkTime,
    TimeSpan RemainingTime,
    int TotalAvailableShifts,
    int SelectedShiftCount,
    double[,] DistanceMatrix,
    double[,] DurationMatrix,
    TimeSpan TravelTimeFromStartBase,
    List<int> RouteIndices,
    double DistanceFromStartBaseKm,
    double DistanceToEndBaseKm,
    TimeSpan TravelTimeToEndBase,
    List<int> FullRouteIndices,
    Dictionary<string, double[,]>? DurationMatricesByProfile = null,
    Klacks.Api.Domain.Enums.ContainerTransportMode TransportMode = Klacks.Api.Domain.Enums.ContainerTransportMode.ByCar,
    List<RouteSegmentDirections>? SegmentDirections = null,
    TimeSpan TotalBriefingDebriefingTime = default);
