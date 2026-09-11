// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Runs the knowledge index synchronization during application startup and reports which retrieval
/// stack the process actually resolved. The sync goes through the scheduler, so it can never run in
/// parallel with one requested by a catalogue change; host start still waits for it. Failures are
/// logged but do not block startup.
/// </summary>
/// <param name="serviceProvider">Root provider used to resolve the scoped retrieval providers for the stack report.</param>
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
        try
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                LogActiveRetrievalStack(scope.ServiceProvider);
            }

            await _syncScheduler.RunNowAsync(KnowledgeIndexSyncConstants.StartupReason, ct);

            var status = _syncScheduler.Status;
            if (status.LastCompletedUtc.HasValue
                && (!status.LastFailedUtc.HasValue || status.LastCompletedUtc >= status.LastFailedUtc))
            {
                _logger.LogInformation("Knowledge index sync completed at startup.");
                return;
            }

            _logger.LogWarning(
                "Knowledge index sync did not complete at startup (last error: {Error}). Skill retrieval is degraded.",
                status.LastError);
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Knowledge index sync failed at startup. Skill retrieval is degraded.");
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
