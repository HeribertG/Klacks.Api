// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Interfaces;

public interface IThoroughRecalculationQueue
{
    bool QueueRecalculation(DateOnly startDate, DateOnly endDate, Guid? selectedGroup, Guid? analyseToken);
}
