// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IHeartbeatLLMService
{
    Task<string?> GenerateStatusMessageAsync(
        IReadOnlyList<HeartbeatCheckItem> checkItems,
        HeartbeatDataSnapshot snapshot,
        CancellationToken cancellationToken = default);
}
