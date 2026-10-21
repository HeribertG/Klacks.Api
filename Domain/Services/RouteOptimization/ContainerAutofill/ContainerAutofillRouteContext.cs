// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Inputs every container autofill phase evaluates a route against.
/// </summary>
/// <param name="DistanceMatrix">Matrix over all candidates, followed by the start base and (when different) the end base</param>
/// <param name="StartBaseIndex">Matrix index of the start base</param>
/// <param name="EndBaseIndex">Matrix index of the end base; equals StartBaseIndex when both bases are the same address</param>
/// <param name="ContainerFromTimeSeconds">Container start as seconds from midnight</param>
/// <param name="ContainerEndTimeSeconds">Container end as seconds from midnight</param>
/// <param name="BudgetSeconds">Time budget minus the duration of all time blocks</param>
/// <param name="TimeRangeTolerance">Relaxation of the shift TimeRange windows (0 or less disables the check)</param>
/// <param name="UnmovableBlocks">Time blocks with a fixed start and end that a stop skips over</param>
/// <param name="MovableBlocks">Time blocks that only reduce the budget</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.ContainerAutofill;

public sealed record ContainerAutofillRouteContext(
    DistanceMatrix DistanceMatrix,
    int StartBaseIndex,
    int EndBaseIndex,
    double ContainerFromTimeSeconds,
    double ContainerEndTimeSeconds,
    double BudgetSeconds,
    double TimeRangeTolerance,
    List<TimeBlock> UnmovableBlocks,
    List<TimeBlock> MovableBlocks);
