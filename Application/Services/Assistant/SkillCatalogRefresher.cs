// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Refreshes every structure derived from the skill catalogue after it changed: the skill cache,
/// the skill registry, and the knowledge index the retrieval pipeline searches. Without the index
/// sync a catalogue change stays invisible to retrieval until the next application start.
/// </summary>
/// <param name="reason">Short description of the catalogue change, used in the failure log line</param>

using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.Application.Services.Assistant;

public interface ISkillCatalogRefresher
{
    Task RefreshAsync(string reason, CancellationToken cancellationToken = default);
}

public class SkillCatalogRefresher : ISkillCatalogRefresher
{
    private readonly ISkillCacheService _skillCacheService;
    private readonly SkillRegistryInitializer _skillRegistryInitializer;
    private readonly IKnowledgeIndexSynchronizer _knowledgeIndexSynchronizer;
    private readonly ILogger<SkillCatalogRefresher> _logger;

    public SkillCatalogRefresher(
        ISkillCacheService skillCacheService,
        SkillRegistryInitializer skillRegistryInitializer,
        IKnowledgeIndexSynchronizer knowledgeIndexSynchronizer,
        ILogger<SkillCatalogRefresher> logger)
    {
        _skillCacheService = skillCacheService;
        _skillRegistryInitializer = skillRegistryInitializer;
        _knowledgeIndexSynchronizer = knowledgeIndexSynchronizer;
        _logger = logger;
    }

    public async Task RefreshAsync(string reason, CancellationToken cancellationToken = default)
    {
        _skillCacheService.InvalidateCache();
        await _skillRegistryInitializer.InitializeAsync(cancellationToken);

        // The catalogue change is already persisted; a failed index sync must not undo it. Retrieval
        // keeps serving the previous index until the next successful sync (startup always runs one).
        try
        {
            await _knowledgeIndexSynchronizer.SyncAsync(cancellationToken);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "Knowledge index sync failed after {Reason}; retrieval serves the stale index until the next successful sync",
                reason);
        }
    }
}
