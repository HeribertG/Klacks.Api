// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Recognises a short messenger reply as "I'm taking this shift", the interim reply format the
/// escalation chain's wake-up text asks for ("...ob du übernimmst") until a provider can carry real
/// inline buttons (docs/ENTWURF-eskalationskette-2026-08-16.md §9, point 2 - explicitly flagged as
/// matching-fragile). AffirmationDetector is deliberately not reused here: its vocabulary is generic
/// yes/ok/go-ahead and does not contain the domain verb "übernehmen"/"take (it)" the wake-up sentence
/// itself uses, so the reference case's own reply ("Ich übernehme") would not match it. The numeric
/// "1" is kept as the pre-buttons fallback the Entwurf documents.
/// </summary>
/// <param name="message">The raw inbound messenger reply text.</param>

using System.Text.RegularExpressions;

namespace Klacks.Api.Domain.Services.Assistant;

public static class EscalationAcknowledgementDetector
{
    private const string NumericShortReply = "1";

    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    private static readonly HashSet<string> AcknowledgementTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        // German
        "übernehme", "uebernehme", "übernehm", "uebernehm", "mach", "geht",
        // English
        "take", "got", "yes", "confirmed",
        // French
        "prends", "occupe",
        // Italian
        "prendo", "penso",
    };

    private static readonly HashSet<string> NegationTokens = new(StringComparer.OrdinalIgnoreCase)
    {
        "nein", "nicht", "kein", "no", "not", "non", "pas",
    };

    public static bool IsAcknowledgement(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return false;
        }

        var trimmed = message.Trim();
        if (trimmed == NumericShortReply)
        {
            return true;
        }

        if (trimmed.Contains('?'))
        {
            return false;
        }

        var tokens = WordPattern.Matches(trimmed)
            .Select(m => m.Value.ToLowerInvariant())
            .ToList();

        if (tokens.Count == 0 || tokens.Any(NegationTokens.Contains))
        {
            return false;
        }

        return tokens.Any(AcknowledgementTokens.Contains);
    }
}
