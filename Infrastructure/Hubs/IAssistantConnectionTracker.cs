// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Infrastructure.Hubs;

public interface IAssistantConnectionTracker
{
    Task RegisterConnectionAsync(string userId, string connectionId, CancellationToken cancellationToken = default);

    Task UnregisterConnectionAsync(string connectionId, CancellationToken cancellationToken = default);

    Task<bool> IsUserConnectedAsync(string userId);

    Task<IReadOnlyList<string>> GetConnectedUserIdsAsync();

    Task<IReadOnlyList<string>> GetConnectionIdsAsync(string userId);
}
