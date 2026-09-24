// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The German texts of the inbound clarification dialog (planner notices, status words and the neutral
/// reply subject), one entry per key of ClarificationTextKeys. Core language, so authored here rather
/// than shipped by a language pack; ClarificationTexts merges the four core languages into its catalogue.
/// </summary>

namespace Klacks.Api.Domain.Constants;

internal static class GermanClarificationTexts
{
    internal static readonly IReadOnlyDictionary<string, string> Texts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ClarificationTextKeys.PlannerStarted] = "💬 **Klärung angefordert** — {sender}\n" +
                "Die Nachricht war unklar: {summary}\n" +
                "Klacksy hat privat nachgefragt: „{question}“\n" +
                "Betroffene Schicht: {shiftContext}\n" +
                "Antwort erwartet bis {deadline}. Du wirst über die Antwort informiert, oder wenn keine rechtzeitig eintrifft.",
        [ClarificationTextKeys.PlannerShiftNone] = "keine im Plan gefunden",
        [ClarificationTextKeys.PlannerAnswerContext] = "💬 Antwort auf Klacksys Frage „{question}“ (gestellt am {asked}).\n" +
                "Ursprüngliche Nachricht: {originalText}",
        [ClarificationTextKeys.PlannerAnswerUnclearNotice] = "⚠️ Die Antwort ist weiterhin unklar. Klacksy fragt nicht ein zweites Mal nach – bitte folge persönlich nach.",
        [ClarificationTextKeys.PlannerExpired] = "⏰ **Frage unbeantwortet** — {sender}\n" +
                "Klacksy hat „{question}“ um {asked} gestellt; bis {deadline} kam keine Antwort.\n" +
                "Ursprüngliche Nachricht: {originalText}\n" +
                "Betroffene Schicht: {shiftContext}\n" +
                "Bitte folge persönlich nach.",
        [ClarificationTextKeys.PlannerSendFailed] = "⚠️ Klacksys Frage „{question}“ konnte nicht gesendet werden. Bitte folge persönlich nach.",
        [ClarificationTextKeys.PlannerSuggested] = "💡 Klacksy schlägt vor, nachzufragen: „{question}“ – Fragen werden nur automatisch ab der globalen Autonomiestufe Assistiert und solange der Notausschalter (Not-Aus für selbständiges Handeln) nicht ausgelöst wurde, versendet.",
        [ClarificationTextKeys.PlannerNoPersonalTarget] = "ℹ️ Die Nachricht ist unklar, aber Klacksy kann nicht nachfragen: Für diesen Mitarbeiter ließ sich auf diesem Kanal kein eindeutiger persönlicher Kontakt feststellen.",
        [ClarificationTextKeys.PlannerAnsweredAfterExpiry] = "ℹ️ Diese Nachricht traf ein, nachdem Klacksys Frage „{question}“ (gestellt am {asked}) unbeantwortet abgelaufen war.",
        [ClarificationTextKeys.PlannerArrivedAfterClosure] = "ℹ️ Diese Nachricht traf ein, nachdem Klacksys Frage „{question}“ (gestellt am {asked}) bereits geschlossen war: {status}.",
        [ClarificationTextKeys.StatusOpen] = "wartet auf Antwort des Mitarbeiters",
        [ClarificationTextKeys.StatusAnswered] = "vom Mitarbeiter beantwortet",
        [ClarificationTextKeys.StatusUnresolved] = "beantwortet, aber weiterhin unklar",
        [ClarificationTextKeys.StatusExpired] = "nicht rechtzeitig beantwortet",
        [ClarificationTextKeys.StatusTakenOver] = "von einem Planer übernommen",
        [ClarificationTextKeys.StatusSuggested] = "nur den Planern vorgeschlagen, nicht gesendet",
        [ClarificationTextKeys.StatusUndelivered] = "die Frage konnte nicht zugestellt werden",
        [ClarificationTextKeys.StatusUnknown] = "unbekannt",
        [ClarificationTextKeys.MailNeutralReplySubject] = "Re: Deine Nachricht an das Planungsteam"
    };
}
