// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// An absence (break) item of a container template: fixed when both StartItem and EndItem are set, movable otherwise.
/// </summary>
/// <param name="TimeRangeStartItem">Start used for the duration of a movable break (00:00 when missing)</param>
/// <param name="TimeRangeEndItem">End used for the duration of a movable break (00:30 when missing)</param>

namespace Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

public record ContainerAbsenceItem(
    Guid AbsenceId,
    TimeOnly? StartItem,
    TimeOnly? EndItem,
    TimeOnly? TimeRangeStartItem,
    TimeOnly? TimeRangeEndItem);
