// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure word-error-rate calculator for STT transcripts: word-level Levenshtein distance
/// (substitutions, insertions, deletions) over the reference word count, computed on
/// accent-, case- and punctuation-insensitive tokens. Also scores name accuracy (share of
/// expected names found as normalized substrings) and the weighted composite of both.
/// </summary>
/// <param name="reference">Expected transcript text of the goldset item</param>
/// <param name="hypothesis">Actual transcript text returned by the STT provider</param>
/// <param name="expectedNames">Proper names that must appear in the transcript</param>

using Klacks.Api.Application.Skills;

namespace Klacks.Api.Application.Services.Assistant.Evaluation.SpeechEval;

public static class WordErrorRate
{
    private const double WerWeight = 0.7;
    private const double NameAccuracyWeight = 0.3;
    private const double MinRate = 0.0;
    private const double MaxRate = 1.0;

    public static double Compute(string? reference, string? hypothesis)
    {
        var referenceTokens = NameMatching.Tokenize(NameMatching.Normalize(reference));
        var hypothesisTokens = NameMatching.Tokenize(NameMatching.Normalize(hypothesis));

        if (referenceTokens.Length == 0)
        {
            return hypothesisTokens.Length == 0 ? MinRate : MaxRate;
        }

        var distance = WordLevenshtein(referenceTokens, hypothesisTokens);
        return (double)distance / referenceTokens.Length;
    }

    public static double ComputeNameAccuracy(string? transcript, IReadOnlyCollection<string> expectedNames)
    {
        if (expectedNames.Count == 0)
        {
            return MaxRate;
        }

        var normalizedTranscript = NameMatching.Normalize(transcript);
        var found = expectedNames.Count(name =>
            normalizedTranscript.Contains(NameMatching.Normalize(name), StringComparison.Ordinal));

        return (double)found / expectedNames.Count;
    }

    public static double ComputeComposite(double wer, double nameAccuracy)
    {
        var clampedWer = Math.Clamp(wer, MinRate, MaxRate);
        return (WerWeight * (MaxRate - clampedWer)) + (NameAccuracyWeight * nameAccuracy);
    }

    private static int WordLevenshtein(string[] reference, string[] hypothesis)
    {
        var previous = new int[hypothesis.Length + 1];
        var current = new int[hypothesis.Length + 1];

        for (var j = 0; j <= hypothesis.Length; j++)
        {
            previous[j] = j;
        }

        for (var i = 1; i <= reference.Length; i++)
        {
            current[0] = i;
            for (var j = 1; j <= hypothesis.Length; j++)
            {
                var substitutionCost = reference[i - 1] == hypothesis[j - 1] ? 0 : 1;
                current[j] = Math.Min(
                    Math.Min(current[j - 1] + 1, previous[j] + 1),
                    previous[j - 1] + substitutionCost);
            }

            (previous, current) = (current, previous);
        }

        return previous[hypothesis.Length];
    }
}
