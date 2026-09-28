// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Hubs;

public interface IConnectionDateRangeTracker
{
    Task RegisterConnectionAsync(string connectionId, DateOnly startDate, DateOnly endDate, Guid? analyseToken, CancellationToken cancellationToken = default);

    Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

    Task SetSelectedGroupAsync(string connectionId, Guid? selectedGroupId, CancellationToken cancellationToken = default);

    Task<ScheduleConnectionSnapshot?> GetConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleConnectionSnapshot>> GetConnectionsForDateRangeAsync(DateOnly startDate, DateOnly endDate, Guid? analyseToken, string? excludeConnectionId = null, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<ScheduleConnectionSnapshot>> GetConnectionsForDatesAsync(IReadOnlyCollection<DateOnly> dates, Guid? analyseToken, string? excludeConnectionId = null, CancellationToken cancellationToken = default);

    Task<GroupedScheduleConnections> GetConnectionsGroupedBySelectedGroupAsync(Guid? analyseToken, CancellationToken cancellationToken = default);
}
