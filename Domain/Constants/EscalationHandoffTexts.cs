// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The two short sentences EscalationNotifier sends outside the wake-up path: a confirmation back to
/// whoever just acknowledged a chain, and a quiet note to the stage that was notified before them.
/// Deliberately separate from MessengerProactiveTexts, whose own test enforces a strict 1:1 with
/// MessengerWakeUpPolicy - neither of these two messages is a wake-up alert, so they do not belong
/// in that bijection.
/// </summary>

using Klacks.Api.Domain.Common;

namespace Klacks.Api.Domain.Constants;

public static class EscalationHandoffTexts
{
    private const string German = "de";
    private const string English = "en";
    private const string French = "fr";
    private const string Italian = "it";

    public const string AcknowledgedConfirmation = "escalation.acknowledgedConfirmation";
    public const string HandoffQuietNote = "escalation.handoffQuietNote";

    private static readonly IReadOnlyDictionary<string, IReadOnlyDictionary<string, string>> Texts =
        new Dictionary<string, IReadOnlyDictionary<string, string>>(StringComparer.Ordinal)
        {
            [AcknowledgedConfirmation] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Danke, du übernimmst den Dienst am {{date}} von {{employee}}.",
                [English] = "Thanks, you're now covering the {{date}} shift for {{employee}}.",
                [French] = "Merci, tu reprends le service du {{date}} pour {{employee}}.",
                [Italian] = "Grazie, ora copri il turno del {{date}} per {{employee}}."
            },
            [HandoffQuietNote] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "{{responder}} hat den Dienst am {{date}} von {{employee}} übernommen.",
                [English] = "{{responder}} has taken over the {{date}} shift for {{employee}}.",
                [French] = "{{responder}} a repris le service du {{date}} pour {{employee}}.",
                [Italian] = "{{responder}} ha rilevato il turno del {{date}} per {{employee}}."
            }
        };

    public static bool TryGetText(string key, string language, out string text)
    {
        text = string.Empty;
        if (string.IsNullOrWhiteSpace(key) || !Texts.TryGetValue(key, out var byLanguage))
        {
            return false;
        }

        if (!string.IsNullOrWhiteSpace(language) && byLanguage.TryGetValue(language, out var localized))
        {
            text = localized;
            return true;
        }

        if (byLanguage.TryGetValue(LanguageConfig.DefaultLanguageFallback, out var fallback))
        {
            text = fallback;
            return true;
        }

        return false;
    }
}
