// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

public interface IAssistantConnectionTracker
{
    Task RegisterConnectionAsync(string userId, string connectionId, CancellationToken cancellationToken = default);

    Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

    Task<bool> IsUserConnectedAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetConnectedUserIdsAsync(CancellationToken cancellationToken = default);

    Task<IReadOnlyList<string>> GetConnectionIdsAsync(string userId, CancellationToken cancellationToken = default);

    Task<IReadOnlyDictionary<string, IReadOnlyList<string>>> GetConnectionIdsByUserAsync(IReadOnlyCollection<string> userIds, CancellationToken cancellationToken = default);
}
