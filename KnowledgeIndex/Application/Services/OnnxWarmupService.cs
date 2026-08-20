// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using System.Diagnostics;
using Klacks.Api.KnowledgeIndex.Application.Constants;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

/// <summary>
/// Builds the ONNX inference sessions once after startup instead of leaving them to the first chat
/// request. Both providers initialize lazily behind a double-checked lock, and nothing in the startup
/// path touches the reranker at all - KnowledgeIndexSynchronizer only embeds when the index actually
/// changed, and never reranks. Whoever asks the assistant the first question after a restart therefore
/// paid for reading ~674 MB of model files and constructing two sessions.
/// Runs as a BackgroundService so it does not delay the host becoming healthy, and swallows its own
/// failures: a warm-up is an optimization, never a reason for the application not to start.
/// </summary>
/// <param name="serviceProvider">Root provider used to resolve the singleton ONNX providers.</param>
/// <param name="configuration">Holds the opt-out flag for memory-constrained hosts.</param>
/// <param name="logger">Reports how long each session took, which is the only place these numbers surface.</param>
public sealed class OnnxWarmupService : BackgroundService
{
    // Short and language-neutral: this text is thrown away, it only has to make the graph run.
    private const string WarmupQuery = "warmup";
    private const string WarmupCandidate = "warmup candidate text";

    private readonly IServiceProvider _serviceProvider;
    private readonly IConfiguration _configuration;
    private readonly ILogger<OnnxWarmupService> _logger;

    public OnnxWarmupService(
        IServiceProvider serviceProvider,
        IConfiguration configuration,
        ILogger<OnnxWarmupService> logger)
    {
        _serviceProvider = serviceProvider;
        _configuration = configuration;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        if (!_configuration.GetValue(KnowledgeIndexConstants.WarmupEnabledConfigKey, true))
        {
            _logger.LogInformation("ONNX warm-up disabled; the first chat request will build the sessions.");
            return;
        }

        try
        {
            using var scope = _serviceProvider.CreateScope();

            await WarmEmbeddingAsync(scope.ServiceProvider, stoppingToken);
            await WarmRerankerAsync(scope.ServiceProvider, stoppingToken);
        }
        catch (OperationCanceledException)
        {
            // Shutdown during warm-up is not a failure.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(
                ex,
                "ONNX warm-up failed. The sessions will be built on the first request instead. " +
                "On a memory-capped host, set {WarmupConfigKey} to false if this is an out-of-memory condition.",
                KnowledgeIndexConstants.WarmupEnabledConfigKey);
        }
    }

    private async Task WarmEmbeddingAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var embedding = scoped.GetService<IEmbeddingProvider>();
        if (embedding == null)
        {
            return;
        }

        var watch = Stopwatch.StartNew();
        await embedding.EmbedQueryAsync(WarmupQuery, ct);
        _logger.LogInformation(
            "ONNX warm-up: embedding session ready in {Ms}ms ({EmbeddingSpace}).",
            watch.ElapsedMilliseconds,
            embedding.EmbeddingSpaceId);
    }

    private async Task WarmRerankerAsync(IServiceProvider scoped, CancellationToken ct)
    {
        var reranker = scoped.GetService<IRerankerProvider>();
        if (reranker == null)
        {
            return;
        }

        var watch = Stopwatch.StartNew();
        await reranker.ScoreAsync(WarmupQuery, [WarmupCandidate], ct);
        _logger.LogInformation(
            "ONNX warm-up: reranker session ready in {Ms}ms ({Reranker}).",
            watch.ElapsedMilliseconds,
            reranker.GetType().Name);
    }
}
