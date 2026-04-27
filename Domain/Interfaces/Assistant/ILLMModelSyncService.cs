// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Synchronizes LLM models from provider APIs against the local database.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILLMModelSyncService
{
    Task SyncAllProvidersAsync(CancellationToken cancellationToken = default);
}
