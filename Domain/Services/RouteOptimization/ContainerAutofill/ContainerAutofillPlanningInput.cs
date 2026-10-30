// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

/// <summary>
/// What the planning stage needs from a request, whether it is a real run or a dry run.
/// </summary>
/// <param name="StartBase">Start address of the route</param>
/// <param name="EndBase">End address of the route</param>
/// <param name="FromTime">Start of the time window</param>
/// <param name="UntilTime">End of the time window</param>
/// <param name="TransportMode">Transport mode for the route calculation</param>
/// <param name="TimeRangeTolerance">Strictness of the task TimeRange windows, see ContainerAutofillRequest</param>
/// <param name="TimeBlocks">Time blocks (breaks) of the route; null = none</param>
/// <param name="AdditionalAvailableWorkIds">Tasks the route must lock in</param>
/// <param name="CancellationToken">Token for cancelling the base geocoding</param>
public sealed record ContainerAutofillPlanningInput(
    string StartBase,
    string EndBase,
    TimeOnly FromTime,
    TimeOnly UntilTime,
    ContainerTransportMode TransportMode,
    double TimeRangeTolerance,
    List<TimeBlock>? TimeBlocks,
    IReadOnlyCollection<Guid>? AdditionalAvailableWorkIds,
    CancellationToken CancellationToken);
