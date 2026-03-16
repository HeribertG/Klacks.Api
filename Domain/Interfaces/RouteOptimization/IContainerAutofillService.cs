// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Service für die automatische Befüllung von Container-Templates mit passenden Shifts.
/// Löst ein Orienteering Problem: Wähle Teilmenge von Shifts innerhalb des Zeitbudgets und optimiere die Route.
/// </summary>
/// <param name="containerId">ID des Containers</param>
/// <param name="weekday">Wochentag (0=So, 1=Mo, ..., 6=Sa)</param>
/// <param name="isHoliday">Ob Feiertagsvariante</param>
/// <param name="startBase">Startadresse der Route</param>
/// <param name="endBase">Endadresse der Route</param>
/// <param name="transportMode">Transportmittel für die Routenberechnung</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Services.RouteOptimization;

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public interface IContainerAutofillService
{
    Task<ContainerAutofillResult> AutofillAsync(
        Guid containerId,
        int weekday,
        bool isHoliday,
        string startBase,
        string endBase,
        TimeOnly fromTime,
        TimeOnly untilTime,
        ContainerTransportMode transportMode = ContainerTransportMode.ByCar,
        CancellationToken cancellationToken = default);
}
