// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Maps the free-text answers of the setup consultation onto enums, deterministically and without a
/// model call. The order of the checks is the point: an exact chip value wins first, then a NEGATED
/// domain noun (its meaning flips: a negated customer noun means None, a negated ERP noun means
/// Manual), then a plain domain noun, then an unclear marker, then a leading negation, and only
/// afterwards a plain affirmation — AffirmationDetector's token set contains "bitte", "mach" and
/// "gerne", so "Bitte intern" would otherwise come out as a yes. A domain noun is read as negated
/// only when a negation marker sits within the two tokens directly before it ("Wir haben keine
/// Kunden"), not anywhere earlier in the message, so a genuine customer answer several words after
/// an unrelated "nicht" is not misread. The unclear-marker check sits before the negation fallback
/// because DeclineDetector treats "kein"/"keine"/"keinen" as a leading negation, so "Keine Ahnung"
/// ("no idea") would otherwise be misread as a concrete No for attribution or a Manual order source.
/// Anything that matches nothing stays Unknown, which every caller must treat as the cautious path
/// rather than as a default answer — landing on None is expensive because it seals a clientless
/// service immediately and irreversibly, while landing on Unknown only re-asks.
/// </summary>
/// <param name="message">The raw slot value the recipe captured from the user's reply.</param>

using System.Text.RegularExpressions;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Services.Assistant;

public static class SetupConsultationAnswerClassifier
{
    private const string ChipYes = "yes";
    private const string ChipNo = "no";
    private const string ChipUnknown = "unknown";
    private const string ChipCreate = "create";
    private const string ChipShow = "show";
    private const string ChipNone = "none";

    private const int NegationLookback = 2;

    private static readonly Regex WordPattern = new(@"\p{L}+", RegexOptions.Compiled);

    public static SetupAttributionAnswer ClassifyAttribution(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return SetupAttributionAnswer.Unknown;
        }

        var trimmed = message.Trim();
        if (trimmed.Equals(ChipYes, StringComparison.OrdinalIgnoreCase))
        {
            return SetupAttributionAnswer.Customer;
        }

        if (trimmed.Equals(ChipNo, StringComparison.OrdinalIgnoreCase))
        {
            return SetupAttributionAnswer.None;
        }

        if (trimmed.Equals(ChipUnknown, StringComparison.OrdinalIgnoreCase))
        {
            return SetupAttributionAnswer.Unknown;
        }

        var tokens = Tokenize(trimmed);

        if (HasNegatedMatch(tokens, SetupConsultationKeywords.AttributedToCustomer))
        {
            return SetupAttributionAnswer.None;
        }

        if (tokens.Any(SetupConsultationKeywords.AttributedToNobody.Contains))
        {
            return SetupAttributionAnswer.None;
        }

        if (tokens.Any(SetupConsultationKeywords.AttributedToCustomer.Contains))
        {
            return SetupAttributionAnswer.Customer;
        }

        if (tokens.Any(SetupConsultationKeywords.UnclearMarkers.Contains))
        {
            return SetupAttributionAnswer.Unknown;
        }

        if (DeclineDetector.LeadsWithNegation(trimmed))
        {
            return SetupAttributionAnswer.None;
        }

        return AffirmationDetector.IsAffirmation(trimmed)
            ? SetupAttributionAnswer.Customer
            : SetupAttributionAnswer.Unknown;
    }

    public static SetupOrderSourceAnswer ClassifyOrderSource(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return SetupOrderSourceAnswer.Unknown;
        }

        var trimmed = message.Trim();
        if (trimmed.Equals(ChipYes, StringComparison.OrdinalIgnoreCase))
        {
            return SetupOrderSourceAnswer.External;
        }

        if (trimmed.Equals(ChipNo, StringComparison.OrdinalIgnoreCase))
        {
            return SetupOrderSourceAnswer.Manual;
        }

        if (trimmed.Equals(ChipUnknown, StringComparison.OrdinalIgnoreCase))
        {
            return SetupOrderSourceAnswer.Unknown;
        }

        var tokens = Tokenize(trimmed);

        if (HasNegatedMatch(tokens, SetupConsultationKeywords.OrderSourceExternal))
        {
            return SetupOrderSourceAnswer.Manual;
        }

        if (tokens.Any(SetupConsultationKeywords.OrderSourceExternal.Contains))
        {
            return SetupOrderSourceAnswer.External;
        }

        if (tokens.Any(SetupConsultationKeywords.OrderSourceManual.Contains))
        {
            return SetupOrderSourceAnswer.Manual;
        }

        if (tokens.Any(SetupConsultationKeywords.UnclearMarkers.Contains))
        {
            return SetupOrderSourceAnswer.Unknown;
        }

        if (DeclineDetector.LeadsWithNegation(trimmed))
        {
            return SetupOrderSourceAnswer.Manual;
        }

        return AffirmationDetector.IsAffirmation(trimmed)
            ? SetupOrderSourceAnswer.External
            : SetupOrderSourceAnswer.Unknown;
    }

    public static SetupNextStepChoice ClassifyNextStep(string? message)
    {
        if (string.IsNullOrWhiteSpace(message))
        {
            return SetupNextStepChoice.Unknown;
        }

        var trimmed = message.Trim();
        if (trimmed.Equals(ChipCreate, StringComparison.OrdinalIgnoreCase))
        {
            return SetupNextStepChoice.Create;
        }

        if (trimmed.Equals(ChipShow, StringComparison.OrdinalIgnoreCase))
        {
            return SetupNextStepChoice.Show;
        }

        if (trimmed.Equals(ChipNone, StringComparison.OrdinalIgnoreCase))
        {
            return SetupNextStepChoice.None;
        }

        if (DeclineDetector.LeadsWithNegation(trimmed))
        {
            return SetupNextStepChoice.None;
        }

        var tokens = Tokenize(trimmed);

        if (tokens.Any(SetupConsultationKeywords.NextStepCreate.Contains))
        {
            return SetupNextStepChoice.Create;
        }

        return tokens.Any(SetupConsultationKeywords.NextStepShow.Contains)
            ? SetupNextStepChoice.Show
            : SetupNextStepChoice.Unknown;
    }

    private static List<string> Tokenize(string message) =>
        WordPattern.Matches(message)
            .Select(match => match.Value)
            .ToList();

    private static bool HasNegatedMatch(IReadOnlyList<string> tokens, HashSet<string> domainNouns)
    {
        for (var i = 0; i < tokens.Count; i++)
        {
            if (!domainNouns.Contains(tokens[i]))
            {
                continue;
            }

            var windowStart = Math.Max(0, i - NegationLookback);
            for (var j = windowStart; j < i; j++)
            {
                if (SetupConsultationKeywords.NegationMarkers.Contains(tokens[j]))
                {
                    return true;
                }
            }
        }

        return false;
    }
}
