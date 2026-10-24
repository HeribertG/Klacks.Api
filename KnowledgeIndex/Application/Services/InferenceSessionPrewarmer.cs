// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// See IInferenceSessionPrewarmer. OnnxSessionIdleUnloadService releases both sessions after the idle window
/// (30 minutes by default), and the first question afterwards paid for rebuilding them on its own critical path:
/// 2.2-2.9 s for the embedder plus 1.1 s for the reranker (measured, see KnowledgeRetrievalService). The chat UI
/// already calls chat/warmup when the assistant panel opens, so that is where the rebuild now starts - while the
/// user is still typing. The rebuild counts as a use, so a panel opened without a question is released again by
/// the same sweep one idle window later; nothing stays loaded longer than after a real question.
/// Fire-and-forget and single-flight: a second request while one runs is a no-op, and a failure is only logged,
/// because the next question builds the sessions itself exactly as before.
/// </summary>
/// <param name="sessions">The unloadable ONNX sessions; empty on a host without ONNX, where nothing is cold.</param>
/// <param name="scopeFactory">Resolves the embedding and reranker providers the way a chat request does.</param>
/// <param name="lifetime">Stops a rebuild that is still running at shutdown.</param>
/// <param name="logger">Reports how long the rebuild took.</param>

using System.Diagnostics;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Klacks.Api.KnowledgeIndex.Application.Services;

public sealed class InferenceSessionPrewarmer : IInferenceSessionPrewarmer
{
    // Short and language-neutral: the vector is thrown away, the call only has to build the session.
    private const string WarmupQuery = "warmup";

    private const int Idle = 0;
    private const int Running = 1;

    private readonly IReadOnlyList<IUnloadableInferenceSession> _sessions;
    private readonly IServiceScopeFactory _scopeFactory;
    private readonly IHostApplicationLifetime _lifetime;
    private readonly ILogger<InferenceSessionPrewarmer> _logger;
    private int _state = Idle;

    public InferenceSessionPrewarmer(
        IEnumerable<IUnloadableInferenceSession> sessions,
        IServiceScopeFactory scopeFactory,
        IHostApplicationLifetime lifetime,
        ILogger<InferenceSessionPrewarmer> logger)
    {
        _sessions = sessions.ToList();
        _scopeFactory = scopeFactory;
        _lifetime = lifetime;
        _logger = logger;
    }

    public bool RequestPrewarm()
    {
        if (_sessions.All(session => session.IsLoaded))
        {
            return false;
        }

        if (Interlocked.CompareExchange(ref _state, Running, Idle) != Idle)
        {
            return false;
        }

        _ = Task.Run(PrewarmAsync);
        return true;
    }

    /// <summary>The background rebuild itself; awaited by tests through the task RequestPrewarm discards.</summary>
    internal async Task PrewarmAsync()
    {
        var watch = Stopwatch.StartNew();
        var cancellationToken = _lifetime.ApplicationStopping;
        try
        {
            using var scope = _scopeFactory.CreateScope();

            var embedding = scope.ServiceProvider.GetService<IEmbeddingProvider>();
            if (embedding != null)
            {
                await embedding.EmbedQueryAsync(WarmupQuery, cancellationToken);
            }

            var reranker = scope.ServiceProvider.GetService<IRerankerProvider>();
            if (reranker != null)
            {
                await reranker.EnsureLoadedAsync(cancellationToken);
            }

            _logger.LogInformation(
                "Inference sessions rebuilt ahead of the next question in {Ms}ms.", watch.ElapsedMilliseconds);
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            // Shutdown during the rebuild is not a failure.
        }
        catch (Exception ex)
        {
            _logger.LogWarning(ex,
                "Rebuilding the inference sessions ahead of the next question failed; the question builds them itself.");
        }
        finally
        {
            Volatile.Write(ref _state, Idle);
        }
    }
}
