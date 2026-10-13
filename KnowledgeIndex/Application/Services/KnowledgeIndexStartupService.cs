// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Starts the knowledge index synchronization during application startup and reports which retrieval
/// stack the process actually resolved. The sync goes through the single-flight scheduler, so it can never
/// run in parallel with one requested by a catalogue change, and it always runs in the background: on a
/// first start it embeds the whole catalogue (and downloads the embedding model), which kept the host - and
/// with it the login page and the Windows service start - waiting for half an hour on a desktop PC.
/// While the index covers less than MinimumCoverageForFullRetrieval of the catalogue, retrieval answers
/// from the always-on and deterministic skills only; that state is logged as a warning.
/// </summary>
/// <param name="serviceProvider">Root provider used to resolve the scoped retrieval providers and the index coverage probe.</param>
/// <param name="syncScheduler">Single-flight scheduler that executes the sync.</param>
/// <param name="logger">Logger for startup diagnostics and error reporting.</param>
public sealed class KnowledgeIndexStartupService : IHostedService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IKnowledgeIndexSyncScheduler _syncScheduler;
    private readonly ILogger<KnowledgeIndexStartupService> _logger;

    public KnowledgeIndexStartupService(
        IServiceProvider serviceProvider,
        IKnowledgeIndexSyncScheduler syncScheduler,
        ILogger<KnowledgeIndexStartupService> logger)
    {
        _serviceProvider = serviceProvider;
        _syncScheduler = syncScheduler;
        _logger = logger;
    }

    public async Task StartAsync(CancellationToken ct)
    {
        double? coverage;
        await using (var scope = _serviceProvider.CreateAsyncScope())
        {
            LogActiveRetrievalStack(scope.ServiceProvider);
            coverage = await ReadCoverageAsync(scope.ServiceProvider, ct);
        }

        _syncScheduler.Request(KnowledgeIndexSyncConstants.StartupReason);

        if (coverage < KnowledgeIndexSyncConstants.MinimumCoverageForFullRetrieval)
        {
            _logger.LogWarning(
                "Knowledge index covers {Coverage:P0} of the catalogue; it is built in the background and skill retrieval is degraded until the sync completes.",
                coverage);
            return;
        }

        _logger.LogInformation(
            "Knowledge index startup sync runs in the background; retrieval serves the stored vectors meanwhile.");
    }

    // Null when the coverage cannot be determined (no probe registered, or the table is unreadable).
    private async Task<double?> ReadCoverageAsync(IServiceProvider scoped, CancellationToken ct)
    {
        try
        {
            var probe = scoped.GetService<IKnowledgeIndexCoverageProbe>();
            if (probe == null)
            {
                return null;
            }

            return await probe.GetStoredCoverageAsync(ct);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "Could not read the knowledge index before the startup sync; it runs in the background.");
            return null;
        }
    }

    // Which embedding and reranker actually got resolved is invisible at runtime, yet it decides
    // retrieval quality: the ONNX pair is the production stack, everything else is a fallback that
    // scores differently. Silently degrading here once cost a full measurement round that compared
    // an unrepresentative stack against itself, so a non-local stack is reported as a warning.
    private void LogActiveRetrievalStack(IServiceProvider scoped)
    {
        var embedding = scoped.GetService<IEmbeddingProvider>();
        var reranker = scoped.GetService<IRerankerProvider>();

        if (embedding == null)
        {
            _logger.LogWarning("Retrieval stack: no embedding provider resolved. Semantic skill retrieval is disabled.");
            return;
        }

        var rerankerName = reranker?.GetType().Name ?? "none";

        if (embedding.EmbeddingSpaceId.StartsWith(KnowledgeIndexConstants.LocalEmbeddingSpacePrefix, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "Retrieval stack: embedding={EmbeddingSpace}, reranker={Reranker}.",
                embedding.EmbeddingSpaceId,
                rerankerName);
            return;
        }

        _logger.LogWarning(
            "Retrieval stack is on the FALLBACK path: embedding={EmbeddingSpace}, reranker={Reranker}. " +
            "The local ONNX stack did not load, so retrieval quality and any measurement taken here " +
            "differ from production. Check {OnnxConfigKey} and the ONNX runtime availability.",
            embedding.EmbeddingSpaceId,
            rerankerName,
            KnowledgeIndexConstants.OnnxEnabledConfigKey);
    }

    public Task StopAsync(CancellationToken ct) => Task.CompletedTask;
}
