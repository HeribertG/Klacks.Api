// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parameter object of the container autofill dry run. The tasks are loaded by the caller, so a caller with limited
/// visibility hands in only what it may see and hidden tasks never enter the calculation.
/// </summary>
/// <param name="Tasks">Tasks a container would be offered; those without IsTimeRange are skipped as in the real run</param>
/// <param name="StartBase">Start address of the route</param>
/// <param name="EndBase">End address of the route</param>
/// <param name="FromTime">Start of the time window</param>
/// <param name="UntilTime">End of the time window</param>
/// <param name="TransportMode">Transport mode for the route calculation</param>
/// <param name="TimeRangeTolerance">Strictness of the task TimeRange windows, see ContainerAutofillRequest</param>
/// <param name="MaxGeocodedAddresses">Upper limit of addresses without stored coordinates sent to the geocoder</param>
/// <param name="CancellationToken">Token for cancelling the operation</param>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record ContainerAutofillPreviewRequest(
    IReadOnlyList<Shift> Tasks,
    string StartBase,
    string EndBase,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    ContainerTransportMode TransportMode,
    double TimeRangeTolerance,
    int MaxGeocodedAddresses,
    CancellationToken CancellationToken = default);
