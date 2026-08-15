// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

public interface IConnectionDateRangeTracker
{
    Task RegisterConnectionAsync(string connectionId, DateOnly startDate, DateOnly endDate, Guid? analyseToken, CancellationToken cancellationToken = default);

    Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

    Task SetSelectedGroupAsync(string connectionId, Guid? selectedGroupId, CancellationToken cancellationToken = default);

    Task SetAnalyseTokenAsync(string connectionId, Guid? analyseToken, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetConnectionsForDateAsync(DateOnly date, Guid? analyseToken, string? excludeConnectionId = null);

    Task<IReadOnlyList<string>> GetConnectionsForDateRangeAsync(DateOnly startDate, DateOnly endDate, Guid? analyseToken, string? excludeConnectionId = null);

    Task<(List<string> AllGroupConnections, Dictionary<Guid, List<string>> GroupConnections)> GetConnectionsGroupedBySelectedGroupAsync(Guid? analyseToken);

    Task<(DateOnly Start, DateOnly End)?> GetRegisteredDateRangeAsync(string connectionId);

    Task<Guid?> GetSelectedGroupAsync(string connectionId);

    Task<Guid?> GetAnalyseTokenAsync(string connectionId);
}
