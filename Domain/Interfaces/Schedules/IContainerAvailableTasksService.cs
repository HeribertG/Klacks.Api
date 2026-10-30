// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Interfaces.Schedules;

public interface IContainerAvailableTasksService
{
    Task<List<Shift>> GetAvailableTasksAsync(
        Guid containerId,
        int weekday,
        TimeOnly fromTime,
        TimeOnly untilTime,
        string? searchString = null,
        Guid? excludeContainerId = null,
        bool? isHoliday = null,
        bool? isWeekdayAndHoliday = null,
        IReadOnlyCollection<Guid>? additionalAvailableWorkIds = null,
        IReadOnlyCollection<Guid>? restrictToShiftIds = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// The candidates of a normal (non-holiday) slot for the direct shifts of the given groups, valid during the given
    /// period, minus the tasks used in any container template, read through the query GetAvailableTasksAsync uses for a
    /// container with those groups and that validity. For callers that have to know the candidates before a container exists.
    /// </summary>
    /// <param name="groupIds">Groups whose direct shifts are the candidates</param>
    /// <param name="validity">Period the tasks must overlap; null = no period filter</param>
    /// <param name="weekday">Stored weekday of the slot (0=Sunday .. 6=Saturday)</param>
    /// <param name="fromTime">Start of the slot's time frame</param>
    /// <param name="untilTime">End of the slot's time frame</param>
    /// <param name="restrictToShiftIds">Optional: only these tasks, intersected with the group's shifts; null = no restriction</param>
    /// <param name="cancellationToken">Cancels the query</param>
    Task<List<Shift>> GetAvailableTasksOfGroupsAsync(
        IReadOnlyCollection<Guid> groupIds,
        ShiftValidityPeriod? validity,
        int weekday,
        TimeOnly fromTime,
        TimeOnly untilTime,
        IReadOnlyCollection<Guid>? restrictToShiftIds = null,
        CancellationToken cancellationToken = default);
}
