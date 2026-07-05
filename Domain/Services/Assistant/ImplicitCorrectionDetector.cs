// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Precision-biased detector that decides whether a user chat message reads as a correction of
/// the assistant's immediately preceding turn ("nein", "nicht das", "falsch", "no, wrong") rather
/// than a fresh, unrelated request. Word matching alone is deliberately too broad to use on its
/// own (common words like "nicht" appear in many ordinary German sentences) — the caller must
/// combine this signal with a short time window since the previous turn, so an implicit
/// correction is only inferred for a reactive, immediate follow-up, not any later message that
/// happens to contain a negation.
/// </summary>
/// <param name="message">The raw user message that started the follow-up turn.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class ImplicitCorrectionDetector
{
    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly HashSet<string> CorrectionTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // German
        "nein", "nicht", "falsch", "falsche", "falscher", "falsches",
        // English
        "no", "not", "wrong", "incorrect",
        // French
        "non", "faux", "incorrect",
        // Italian
        "no", "sbagliato", "errato",
    };

    public static bool IsCorrectionSignal(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var tokens = WordPattern.Matches(message)
            .Select(m => m.Value.ToLowerInvariant());

        return tokens.Any(CorrectionTokens.Contains);
    }
}
