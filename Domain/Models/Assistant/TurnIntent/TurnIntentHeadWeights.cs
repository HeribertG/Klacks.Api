// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Trained turn-intent head as stored in turn-intent-head.v1.json: a multinomial logistic regression over the
/// e5 query embedding of the bare message followed by the TurnIntentHandFeatures, after a per-column
/// standardisation. Bound to one embedding space; a provider with a different space must not use it.
/// </summary>
/// <param name="Version">File format version</param>
/// <param name="EmbeddingSpaceId">Embedding space the head was trained in, e.g. onnx:multilingual-e5-base-fp16@768</param>
/// <param name="DatasetHash">Fingerprint of the labelset the head was trained on</param>
/// <param name="FeatureSourceHash">Fingerprint of the hand-feature sources (detector code and plugin keyword lists)</param>
/// <param name="TrainedAtUtc">Training time, ISO 8601 UTC</param>
/// <param name="Lambda">L2 strength used for training</param>
/// <param name="Iterations">Gradient steps used for training</param>
/// <param name="Classes">Class names in logit order, equal to the TurnIntentLabel names</param>
/// <param name="HandFeatureNames">Names of the hand-feature columns appended after the embedding</param>
/// <param name="Means">Column means subtracted before scaling</param>
/// <param name="Scales">Column factors applied after the mean subtraction</param>
/// <param name="Weights">Class-by-column weight matrix</param>
/// <param name="Bias">Per-class bias</param>
/// <param name="Temperature">Logit divisor fitted on out-of-fold logits</param>
/// <param name="AskThreshold">Minimum Ask probability for a confident Ask</param>
/// <param name="Locales">Per-locale flip evidence and verdict</param>

namespace Klacks.Api.Domain.Models.Assistant.TurnIntent;

public sealed class TurnIntentHeadWeights
{
    public int Version { get; init; }
    public string EmbeddingSpaceId { get; init; } = string.Empty;
    public string DatasetHash { get; init; } = string.Empty;
    public string FeatureSourceHash { get; init; } = string.Empty;
    public string TrainedAtUtc { get; init; } = string.Empty;
    public double Lambda { get; init; }
    public int Iterations { get; init; }
    public string[] Classes { get; init; } = [];
    public string[] HandFeatureNames { get; init; } = [];
    public double[] Means { get; init; } = [];
    public double[] Scales { get; init; } = [];
    public double[][] Weights { get; init; } = [];
    public double[] Bias { get; init; } = [];
    public double Temperature { get; init; } = 1.0;
    public double AskThreshold { get; init; }
    public List<TurnIntentLocaleVerdict> Locales { get; init; } = [];
}
