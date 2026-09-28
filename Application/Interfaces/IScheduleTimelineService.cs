// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces;

public interface IScheduleTimelineService
{
    void QueueCheck(Guid clientId, DateOnly date, Guid? analyseToken);
    void QueueRangeCheck(DateOnly startDate, DateOnly endDate, Guid? analyseToken);
}
