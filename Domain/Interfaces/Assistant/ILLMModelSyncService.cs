// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Synchronizes LLM models from provider APIs against the local database.
/// </summary>
namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILLMModelSyncService
{
    Task SyncAllProvidersAsync(CancellationToken cancellationToken = default);
}
