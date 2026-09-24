// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The Italian texts of the inbound clarification dialog (planner notices, status words and the neutral
/// reply subject), one entry per key of ClarificationTextKeys. Core language, so authored here rather
/// than shipped by a language pack; ClarificationTexts merges the four core languages into its catalogue.
/// </summary>

namespace Klacks.Api.Domain.Constants;

internal static class ItalianClarificationTexts
{
    internal static readonly IReadOnlyDictionary<string, string> Texts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ClarificationTextKeys.PlannerStarted] = "💬 **Chiarimento richiesto** — {sender}\n" +
                "Il messaggio non era chiaro: {summary}\n" +
                "Klacksy ha chiesto in privato: \"{question}\"\n" +
                "Turno interessato: {shiftContext}\n" +
                "Risposta attesa entro {deadline}. Sarai informato sulla risposta, oppure se questa non arriva in tempo.",
        [ClarificationTextKeys.PlannerShiftNone] = "nessuno trovato nel piano",
        [ClarificationTextKeys.PlannerAnswerContext] = "💬 Risposta alla domanda di Klacksy \"{question}\" (posta il {asked}).\n" +
                "Messaggio originale: {originalText}",
        [ClarificationTextKeys.PlannerAnswerUnclearNotice] = "⚠️ La risposta non è ancora chiara. Klacksy non fa una seconda domanda, per favore segui personalmente.",
        [ClarificationTextKeys.PlannerExpired] = "⏰ **Domanda senza risposta** — {sender}\n" +
                "Klacksy ha chiesto \"{question}\" il {asked}; non c'è stata risposta entro {deadline}.\n" +
                "Messaggio originale: {originalText}\n" +
                "Turno interessato: {shiftContext}\n" +
                "Per favore segui personalmente.",
        [ClarificationTextKeys.PlannerSendFailed] = "⚠️ La domanda di Klacksy \"{question}\" non è potuta essere inviata. Per favore segui personalmente.",
        [ClarificationTextKeys.PlannerSuggested] = "💡 Klacksy propone di chiedere: \"{question}\" — le domande vengono inviate automaticamente solo a partire dal livello globale di autonomia Assistito e finché l'interruttore di emergenza (Interruttore di arresto per l'azione autonoma) non è stato attivato.",
        [ClarificationTextKeys.PlannerNoPersonalTarget] = "ℹ️ Il messaggio non è chiaro, ma Klacksy non può chiedere indietro: non è stato possibile determinare un contatto personale univoco per questo dipendente su questo canale.",
        [ClarificationTextKeys.PlannerAnsweredAfterExpiry] = "ℹ️ Questo messaggio è arrivato dopo che la domanda di Klacksy \"{question}\" (posta il {asked}) era scaduta senza risposta.",
        [ClarificationTextKeys.PlannerArrivedAfterClosure] = "ℹ️ Questo messaggio è arrivato dopo che la domanda di Klacksy \"{question}\" (posta il {asked}) era già stata chiusa: {status}.",
        [ClarificationTextKeys.StatusOpen] = "in attesa della risposta del dipendente",
        [ClarificationTextKeys.StatusAnswered] = "risposto dal dipendente",
        [ClarificationTextKeys.StatusUnresolved] = "risposto, ma ancora poco chiaro",
        [ClarificationTextKeys.StatusExpired] = "non risposto in tempo",
        [ClarificationTextKeys.StatusTakenOver] = "preso in carico da un planner",
        [ClarificationTextKeys.StatusSuggested] = "solo suggerito ai planner, non inviato",
        [ClarificationTextKeys.StatusUndelivered] = "la domanda non ha potuto essere consegnata",
        [ClarificationTextKeys.StatusUnknown] = "sconosciuto",
        [ClarificationTextKeys.MailNeutralReplySubject] = "Re: Il tuo messaggio al team di pianificazione"
    };
}
