// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface ITimelineCalculationService
{
    List<TimeRect> CalculateTimeRects(List<Work> works, List<WorkChange> workChanges, List<Break> breaks);
}
