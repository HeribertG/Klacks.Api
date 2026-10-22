// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IScheduleEntriesService
{
    /// <summary>
    /// Reads the schedule rows matching the criteria, materialised, in the function's order (client, work day,
    /// start time, entry type) unless the criteria ask for start-time order.
    /// </summary>
    Task<IReadOnlyList<ScheduleCell>> GetScheduleEntriesAsync(
        ScheduleEntriesCriteria criteria,
        CancellationToken cancellationToken = default);
}
