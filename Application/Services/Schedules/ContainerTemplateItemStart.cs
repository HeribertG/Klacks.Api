// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Start of a container template item in minutes since midnight as ContainerTemplateItemOrder orders items: the time
/// range start of a visit whose shift is a TimeRange shift, the own start of every other item, midnight when unset.
/// </summary>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Domain.Services.RouteOptimization.VisitScheduling;

namespace Klacks.Api.Application.Services.Schedules;

public static class ContainerTemplateItemStart
{
    public static int Minutes(ContainerTemplateItemResource item) =>
        ContainerTemplateItemOrder.StartMinutes(
            !item.AbsenceId.HasValue && item.Shift?.IsTimeRange == true, item.StartItem, item.TimeRangeStartItem);
}
