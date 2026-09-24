// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The French texts of the inbound clarification dialog (planner notices, status words and the neutral
/// reply subject), one entry per key of ClarificationTextKeys. Core language, so authored here rather
/// than shipped by a language pack; ClarificationTexts merges the four core languages into its catalogue.
/// </summary>

namespace Klacks.Api.Domain.Constants;

internal static class FrenchClarificationTexts
{
    internal static readonly IReadOnlyDictionary<string, string> Texts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ClarificationTextKeys.PlannerStarted] = "💬 **Clarification demandée** — {sender}\n" +
                "Le message n’était pas clair : {summary}\n" +
                "Klacksy a posé une question en privé : « {question} »\n" +
                "Service concerné : {shiftContext}\n" +
                "Réponse attendue avant {deadline}. Vous serez informé·e de la réponse, ou de son absence à temps.",
        [ClarificationTextKeys.PlannerShiftNone] = "Aucun service trouvé dans le planning",
        [ClarificationTextKeys.PlannerAnswerContext] = "💬 Réponse à la question de Klacksy « {question} » (posée le {asked}).\n" +
                "Message d’origine : {originalText}",
        [ClarificationTextKeys.PlannerAnswerUnclearNotice] = "⚠️ La réponse reste floue. Klacksy ne pose pas de deuxième question, veuillez relancer personnellement.",
        [ClarificationTextKeys.PlannerExpired] = "⏰ **Question sans réponse** — {sender}\n" +
                "Klacksy a posé « {question} » le {asked} ; aucune réponse n’a été reçue avant {deadline}.\n" +
                "Message d’origine : {originalText}\n" +
                "Service concerné : {shiftContext}\n" +
                "Veuillez relancer personnellement.",
        [ClarificationTextKeys.PlannerSendFailed] = "⚠️ La question de Klacksy « {question} » n’a pas pu être envoyée. Veuillez relancer personnellement.",
        [ClarificationTextKeys.PlannerSuggested] = "💡 Klacksy propose de demander : « {question} » — les questions ne sont envoyées automatiquement qu’à partir du niveau d’autonomie global Assisté et tant que l’interrupteur d’urgence (Interrupteur d'arrêt pour l'action autonome) n’a pas été activé.",
        [ClarificationTextKeys.PlannerNoPersonalTarget] = "ℹ️ Le message est peu clair, mais Klacksy ne peut pas relancer : aucun contact personnel non ambigu n’a pu être identifié pour cet employé sur ce canal.",
        [ClarificationTextKeys.PlannerAnsweredAfterExpiry] = "ℹ️ Ce message est arrivé après l’expiration sans réponse de la question de Klacksy « {question} » (posée le {asked}).",
        [ClarificationTextKeys.PlannerArrivedAfterClosure] = "ℹ️ Ce message est arrivé après la fermeture de la question de Klacksy « {question} » (posée le {asked}) : {status}.",
        [ClarificationTextKeys.StatusOpen] = "en attente de la réponse de l’employé",
        [ClarificationTextKeys.StatusAnswered] = "répondu par l’employé",
        [ClarificationTextKeys.StatusUnresolved] = "répondu, mais toujours flou",
        [ClarificationTextKeys.StatusExpired] = "pas répondu à temps",
        [ClarificationTextKeys.StatusTakenOver] = "repris par un planificateur",
        [ClarificationTextKeys.StatusSuggested] = "seulement suggéré aux planificateurs, non envoyé",
        [ClarificationTextKeys.StatusUndelivered] = "la question n’a pas pu être livrée",
        [ClarificationTextKeys.StatusUnknown] = "inconnu",
        [ClarificationTextKeys.MailNeutralReplySubject] = "Re: Votre message à l’équipe de planification"
    };
}
