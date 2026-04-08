// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Parameter object for automatic container filling.
/// </summary>
/// <param name="ContainerId">ID of the container</param>
/// <param name="Weekday">Weekday (0=Sun, 1=Mon, ..., 6=Sat)</param>
/// <param name="IsHoliday">Whether holiday variant</param>
/// <param name="StartBase">Start address of the route</param>
/// <param name="EndBase">End address of the route</param>
/// <param name="FromTime">Start of the time window</param>
/// <param name="UntilTime">End of the time window</param>
/// <param name="TransportMode">Transport mode for route calculation</param>
/// <param name="TimeRangeTolerance">Tolerance for TimeRange violations (0.0 = strict, 1.0 = no validation)</param>
/// <param name="CancellationToken">Token for cancelling the operation</param>
/// <param name="TimeBlocks">Optional time blocks (breaks) for route optimization</param>

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
    CancellationToken CancellationToken = default,
    List<TimeBlock>? TimeBlocks = null,
    IReadOnlyCollection<Guid>? AdditionalAvailableWorkIds = null);
