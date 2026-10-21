// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Parameter object for automatic container filling.
/// </summary>
/// <param name="ContainerId">ID of the container</param>
/// <param name="Weekday">Weekday (0=Sun, 1=Mon, ..., 6=Sat)</param>
/// <param name="IsHoliday">Whether a public holiday falling on Weekday is filled. Candidates are the tasks the schedule engine
/// runs on that day (ShiftDayActivityFilter): on a holiday IsHoliday tasks and tasks of the weekday with IsWeekdayAndHoliday,
/// on a normal day every task of the weekday; tasks in AdditionalAvailableWorkIds are added regardless</param>
/// <param name="StartBase">Start address of the route</param>
/// <param name="EndBase">End address of the route</param>
/// <param name="FromTime">Start of the time window</param>
/// <param name="UntilTime">End of the time window</param>
/// <param name="TransportMode">Transport mode for route calculation</param>
/// <param name="TimeRangeTolerance">How strictly each task's own TimeRange window is enforced on its planned arrival:
/// 1.0 = strict (arrival inside the window), lower values widen the window on both sides by (1 - value) x its length,
/// 0 or less disables the check</param>
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
