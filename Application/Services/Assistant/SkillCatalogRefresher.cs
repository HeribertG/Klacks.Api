// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Refreshes every structure derived from the skill catalogue after it changed: the skill cache and
/// the skill registry synchronously, and the knowledge index the retrieval pipeline searches through
/// the background sync scheduler. The scheduler owns sync failures, so neither member throws for a
/// failed index sync; retrieval then keeps serving the previous index.
/// </summary>
/// <param name="skillCacheService">Cache of the per-request skill lists, invalidated first</param>
/// <param name="skillRegistryInitializer">Reloads the in-memory skill registry keyword matching reads</param>
/// <param name="knowledgeIndexSyncScheduler">Runs the knowledge index sync outside the caller's request</param>

using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.Application.Services.Assistant;

public class SkillCatalogRefresher : ISkillCatalogRefresher
{
    private readonly ISkillCacheService _skillCacheService;
    private readonly SkillRegistryInitializer _skillRegistryInitializer;
    private readonly IKnowledgeIndexSyncScheduler _knowledgeIndexSyncScheduler;

    public SkillCatalogRefresher(
        ISkillCacheService skillCacheService,
        SkillRegistryInitializer skillRegistryInitializer,
        IKnowledgeIndexSyncScheduler knowledgeIndexSyncScheduler)
    {
        _skillCacheService = skillCacheService;
        _skillRegistryInitializer = skillRegistryInitializer;
        _knowledgeIndexSyncScheduler = knowledgeIndexSyncScheduler;
    }

    public async Task RefreshAsync(string reason, CancellationToken cancellationToken = default)
    {
        await RefreshRegistryAsync(cancellationToken);
        _knowledgeIndexSyncScheduler.Request(reason);
    }

    public async Task RefreshAndWaitForIndexAsync(string reason, CancellationToken cancellationToken = default)
    {
        await RefreshRegistryAsync(cancellationToken);
        await _knowledgeIndexSyncScheduler.RunNowAsync(reason, cancellationToken);
    }

    // The registry has to be current before the sync is requested: the synchronizer builds the index
    // entries from the singleton registry this call reloads.
    private async Task RefreshRegistryAsync(CancellationToken cancellationToken)
    {
        _skillCacheService.InvalidateCache();
        await _skillRegistryInitializer.InitializeAsync(cancellationToken);
    }
}
