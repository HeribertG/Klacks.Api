// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A visit (task shift item) of a container template as the visit scheduler needs it.
/// </summary>
/// <param name="ShiftId">Task shift the item refers to; route steps are matched by it</param>
/// <param name="HasShift">False when the item's shift is not loaded; such an item is never scheduled</param>
/// <param name="WorkTimeHours">Work time of the task shift in hours</param>
/// <param name="TransportMode">Transport mode of the item; decides the minimum travel time</param>

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public record VisitItem(
    Guid ShiftId,
    bool HasShift,
    double WorkTimeHours,
    TimeOnly BriefingTime,
    TimeOnly DebriefingTime,
    TransportMode TransportMode);
