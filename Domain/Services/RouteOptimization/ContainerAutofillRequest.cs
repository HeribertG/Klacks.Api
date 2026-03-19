// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parameter-Objekt fuer die automatische Container-Befuellung.
/// </summary>
/// <param name="ContainerId">ID des Containers</param>
/// <param name="Weekday">Wochentag (0=So, 1=Mo, ..., 6=Sa)</param>
/// <param name="IsHoliday">Ob Feiertagsvariante</param>
/// <param name="StartBase">Startadresse der Route</param>
/// <param name="EndBase">Endadresse der Route</param>
/// <param name="FromTime">Beginn des Zeitfensters</param>
/// <param name="UntilTime">Ende des Zeitfensters</param>
/// <param name="TransportMode">Transportmittel fuer die Routenberechnung</param>
/// <param name="TimeRangeTolerance">Toleranz fuer TimeRange-Verletzungen (0.0 = strikt, 1.0 = keine Pruefung)</param>
/// <param name="CancellationToken">Token zum Abbrechen der Operation</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record ContainerAutofillRequest(
    Guid ContainerId,
    int Weekday,
    bool IsHoliday,
    string StartBase,
    string EndBase,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    ContainerTransportMode TransportMode = ContainerTransportMode.ByCar,
    double TimeRangeTolerance = 0.5,
    CancellationToken CancellationToken = default);
