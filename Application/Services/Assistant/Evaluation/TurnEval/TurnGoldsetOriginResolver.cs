// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Derives the origin of a goldset item from its Source field, the only marker every goldset file carries
/// (the id prefix is not reliable: w05-coverage-analyze-grouping-feasibility is curated, and translations and
/// paraphrases carry the id of their source). The split is by what an item measures: a curated item tests whether the model
/// finds the skill from a message a person wrote, a generated item mostly tests how well the generator
/// phrased it. The w0.5-live items are hand-labelled live misses but carry the generated source prefix and
/// are counted as synthetic, consistent with the weakness analysis that counted every w05- item as such.
/// </summary>
/// <param name="source">The Source field of the goldset item</param>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public static class TurnGoldsetOriginResolver
{
    public static TurnGoldsetOrigin Resolve(string? source)
    {
        if (string.IsNullOrWhiteSpace(source))
        {
            return TurnGoldsetOrigin.Unknown;
        }

        if (string.Equals(source, TurnGoldsetSources.Curated, StringComparison.OrdinalIgnoreCase))
        {
            return TurnGoldsetOrigin.Handwritten;
        }

        if (string.Equals(source, TurnGoldsetSources.Paraphrase, StringComparison.OrdinalIgnoreCase)
            || string.Equals(source, TurnGoldsetSources.Translation, StringComparison.OrdinalIgnoreCase)
            || source.StartsWith(TurnGoldsetSources.SeedGeneratedPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return TurnGoldsetOrigin.Synthetic;
        }

        return TurnGoldsetOrigin.Unknown;
    }
}
