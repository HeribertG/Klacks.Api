// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Runs the trained turn-intent head on the bare user message: one extra e5 query embedding plus the deterministic
/// hand features. The configured timeout cancels waiting for a model slot or a cold session build (in Active mode the
/// call sits on the turn's critical path); an ONNX inference that has already started is not preemptible. Any reason not to trust the result - classifier off, no head, a fallback embedding provider from a
/// different vector space, an embedding error or a timeout - yields null so the caller keeps today's behaviour.
/// </summary>
/// <param name="embeddingProvider">Runtime embedding provider; only its own vector space is accepted</param>
/// <param name="headProvider">Supplies the trained head weights</param>
/// <param name="options">Mode and timeout from Assistant:TurnIntentClassifier</param>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant.TurnIntent;
using Klacks.Api.Domain.Services.Assistant;
using Klacks.Api.KnowledgeIndex.Application.Interfaces;

namespace Klacks.Api.Application.Services.Assistant;

public sealed class TurnIntentClassifier : ITurnIntentClassifier
{
    private readonly IEmbeddingProvider _embeddingProvider;
    private readonly ITurnIntentHeadProvider _headProvider;
    private readonly TurnIntentClassifierOptions _options;
    private readonly ILogger<TurnIntentClassifier> _logger;

    public TurnIntentClassifier(
        IEmbeddingProvider embeddingProvider,
        ITurnIntentHeadProvider headProvider,
        TurnIntentClassifierOptions options,
        ILogger<TurnIntentClassifier> logger)
    {
        _embeddingProvider = embeddingProvider;
        _headProvider = headProvider;
        _options = options;
        _logger = logger;
    }

    public async Task<TurnIntentPrediction?> ClassifyAsync(
        string message, string? language, CancellationToken cancellationToken)
    {
        if (!_options.IsEnabled || string.IsNullOrWhiteSpace(message))
        {
            return null;
        }

        var head = _headProvider.Current;
        if (head == null)
        {
            return null;
        }

        if (!string.Equals(_embeddingProvider.EmbeddingSpaceId, head.EmbeddingSpaceId, StringComparison.Ordinal))
        {
            _logger.LogInformation(
                "{Prefix} skipped: embedding space {Actual} differs from the head's {Expected}",
                TurnIntentDefaults.LogPrefix, _embeddingProvider.EmbeddingSpaceId, head.EmbeddingSpaceId);
            return null;
        }

        float[] embedding;
        using var timeout = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        timeout.CancelAfter(TimeSpan.FromMilliseconds(_options.TimeoutMilliseconds));
        try
        {
            embedding = await _embeddingProvider.EmbedQueryAsync(message, timeout.Token).WaitAsync(timeout.Token);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            _logger.LogInformation(
                "{Prefix} skipped: embedding exceeded {TimeoutMs} ms", TurnIntentDefaults.LogPrefix, _options.TimeoutMilliseconds);
            return null;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            _logger.LogWarning(ex, "{Prefix} skipped: embedding failed", TurnIntentDefaults.LogPrefix);
            return null;
        }

        var handFeatures = TurnIntentHandFeatures.Extract(message);
        if (embedding.Length + handFeatures.Length != head.Means.Length)
        {
            _logger.LogWarning(
                "{Prefix} skipped: embedding dimension {Dimension} does not fit the head", TurnIntentDefaults.LogPrefix, embedding.Length);
            return null;
        }

        var probabilities = TurnIntentHead.Probabilities(head, embedding, handFeatures);
        var label = (TurnIntentLabel)Array.IndexOf(probabilities, probabilities.Max());
        var askProbability = probabilities[(int)TurnIntentLabel.Ask];
        var locale = TurnIntentLocaleGate.ResolveLocale(language, head.Locales);

        return new TurnIntentPrediction(
            label,
            askProbability,
            probabilities[(int)TurnIntentLabel.Do],
            probabilities[(int)TurnIntentLabel.Other],
            label == TurnIntentLabel.Ask && askProbability >= head.AskThreshold,
            locale?.Locale,
            locale?.Enabled == true);
    }
}
