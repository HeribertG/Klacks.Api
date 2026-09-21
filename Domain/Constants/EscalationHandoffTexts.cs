// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The short sentences EscalationNotifier sends outside the wake-up path: per chain purpose a
/// confirmation back to whoever just acknowledged a chain and a quiet note to the stages that were
/// notified before them, plus - for the approval chain only - the note that closes a window nobody used.
/// The absence pair speaks of a shift being covered, the approval texts of a remediation being released
/// or not - both with the responder's name so nobody who was asked earlier acts a
/// second time. Deliberately separate from MessengerProactiveTexts, whose own test enforces a strict 1:1
/// with MessengerWakeUpPolicy - none of these messages is a wake-up alert, so they do not belong in that
/// bijection.
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
    public const string ApprovalAcknowledgedConfirmation = "escalation.approvalAcknowledgedConfirmation";
    public const string ApprovalHandoffQuietNote = "escalation.approvalHandoffQuietNote";
    public const string ApprovalExhaustedNote = "escalation.approvalExhaustedNote";

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
            },
            [ApprovalAcknowledgedConfirmation] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Danke, du hast die Massnahme {{action}} zur Feststellung {{finding}} freigegeben. Klacksy führt sie in Kürze unter deinen Rechten aus und berichtet dir das Ergebnis.",
                [English] = "Thanks, you approved the action {{action}} for the finding {{finding}}. Klacksy will carry it out shortly under your rights and report the result to you.",
                [French] = "Merci, tu as approuvé l'action {{action}} pour le constat {{finding}}. Klacksy va l'exécuter sous peu avec tes droits et te communiquer le résultat.",
                [Italian] = "Grazie, hai approvato l'azione {{action}} per la rilevazione {{finding}}. Klacksy la eseguirà a breve con i tuoi diritti e ti comunicherà il risultato."
            },
            [ApprovalHandoffQuietNote] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "{{responder}} hat die Massnahme {{action}} zur Feststellung {{finding}} freigegeben; du musst nichts mehr tun.",
                [English] = "{{responder}} has approved the action {{action}} for the finding {{finding}}; nothing is left for you to do.",
                [French] = "{{responder}} a approuvé l'action {{action}} pour le constat {{finding}} ; tu n'as plus rien à faire.",
                [Italian] = "{{responder}} ha approvato l'azione {{action}} per la rilevazione {{finding}}; non devi fare altro."
            },
            [ApprovalExhaustedNote] = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase)
            {
                [German] = "Niemand hat die Massnahme {{action}} zur Feststellung {{finding}} freigegeben; die Frist ist abgelaufen. Klacksy fragt am nächsten Firmentag erneut. Soll sie heute noch laufen, delegiere die Feststellung direkt.",
                [English] = "Nobody approved the action {{action}} for the finding {{finding}}; the window has lapsed. Klacksy will ask again on the next company day. To still have it run today, delegate the finding directly.",
                [French] = "Personne n'a approuvé l'action {{action}} pour le constat {{finding}} ; le délai est écoulé. Klacksy redemandera le prochain jour ouvré. Pour qu'elle s'exécute encore aujourd'hui, délègue directement le constat.",
                [Italian] = "Nessuno ha approvato l'azione {{action}} per la rilevazione {{finding}}; il termine è scaduto. Klacksy lo richiederà il prossimo giorno aziendale. Se deve essere eseguita ancora oggi, delega direttamente la rilevazione."
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
