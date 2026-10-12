// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Inference of the trained turn-intent head: concatenates the embedding and the hand features, standardises every
/// column, computes the class logits and returns the temperature-scaled softmax in TurnIntentLabel order.
/// </summary>
/// <param name="weights">The trained head</param>
/// <param name="embedding">e5 query embedding of the bare message</param>
/// <param name="handFeatures">TurnIntentHandFeatures.Extract of the same message</param>

using Klacks.Api.Domain.Models.Assistant.TurnIntent;

namespace Klacks.Api.Domain.Services.Assistant;

public static class TurnIntentHead
{
    public static double[] Probabilities(TurnIntentHeadWeights weights, float[] embedding, double[] handFeatures)
    {
        var columns = embedding.Length + handFeatures.Length;
        if (columns != weights.Means.Length || columns != weights.Scales.Length)
        {
            throw new InvalidOperationException(
                $"Turn-intent head expects {weights.Means.Length} columns but received {columns}.");
        }

        var standardised = new double[columns];
        for (var j = 0; j < embedding.Length; j++)
        {
            standardised[j] = (embedding[j] - weights.Means[j]) * weights.Scales[j];
        }

        for (var j = 0; j < handFeatures.Length; j++)
        {
            var column = embedding.Length + j;
            standardised[column] = (handFeatures[j] - weights.Means[column]) * weights.Scales[column];
        }

        var logits = new double[weights.Weights.Length];
        for (var c = 0; c < logits.Length; c++)
        {
            var sum = weights.Bias[c];
            var row = weights.Weights[c];
            for (var j = 0; j < columns; j++)
            {
                sum += row[j] * standardised[j];
            }

            logits[c] = sum / weights.Temperature;
        }

        return Softmax(logits);
    }

    public static double[] Softmax(double[] logits)
    {
        var max = logits.Max();
        var result = new double[logits.Length];
        var sum = 0.0;
        for (var i = 0; i < logits.Length; i++)
        {
            result[i] = Math.Exp(logits[i] - max);
            sum += result[i];
        }

        for (var i = 0; i < result.Length; i++)
        {
            result[i] /= sum;
        }

        return result;
    }
}
