// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The English texts of the inbound clarification dialog (planner notices, status words and the neutral
/// reply subject), one entry per key of ClarificationTextKeys. Core language, so authored here rather
/// than shipped by a language pack; ClarificationTexts merges the four core languages into its catalogue.
/// </summary>

namespace Klacks.Api.Domain.Constants;

internal static class EnglishClarificationTexts
{
    internal static readonly IReadOnlyDictionary<string, string> Texts = new Dictionary<string, string>(StringComparer.Ordinal)
    {
        [ClarificationTextKeys.PlannerStarted] = "💬 **Clarification requested** — {sender}\n" +
                "The message was unclear: {summary}\n" +
                "Klacksy asked back privately: \"{question}\"\n" +
                "Affected shift: {shiftContext}\n" +
                "Answer expected by {deadline}. You will be informed about the answer, or when none arrives in time.",
        [ClarificationTextKeys.PlannerShiftNone] = "none found in the plan",
        [ClarificationTextKeys.PlannerAnswerContext] = "💬 Answer to Klacksy's question \"{question}\" (asked {asked}).\n" +
                "Original message: {originalText}",
        [ClarificationTextKeys.PlannerAnswerUnclearNotice] = "⚠️ The answer is still unclear. Klacksy does not ask a second time, please follow up personally.",
        [ClarificationTextKeys.PlannerExpired] = "⏰ **Question unanswered** — {sender}\n" +
                "Klacksy asked \"{question}\" at {asked}; there was no answer by {deadline}.\n" +
                "Original message: {originalText}\n" +
                "Affected shift: {shiftContext}\n" +
                "Please follow up personally.",
        [ClarificationTextKeys.PlannerSendFailed] = "⚠️ Klacksy's question \"{question}\" could not be sent. Please follow up personally.",
        [ClarificationTextKeys.PlannerSuggested] = "💡 Klacksy would ask back: \"{question}\" — questions are only sent automatically from the global autonomy level Assisted upwards and as long as the kill switch (Master off switch for self-directed action) has not been triggered.",
        [ClarificationTextKeys.PlannerNoPersonalTarget] = "ℹ️ The message is unclear, but Klacksy cannot ask back: no unambiguous personal contact could be determined for this employee on this channel.",
        [ClarificationTextKeys.PlannerAnsweredAfterExpiry] = "ℹ️ This message arrived after Klacksy's question \"{question}\" (asked {asked}) had expired unanswered.",
        [ClarificationTextKeys.PlannerArrivedAfterClosure] = "ℹ️ This message arrived after Klacksy's question \"{question}\" (asked {asked}) had already been closed: {status}.",
        [ClarificationTextKeys.StatusOpen] = "waiting for the employee's answer",
        [ClarificationTextKeys.StatusAnswered] = "answered by the employee",
        [ClarificationTextKeys.StatusUnresolved] = "answered, but still unclear",
        [ClarificationTextKeys.StatusExpired] = "not answered in time",
        [ClarificationTextKeys.StatusTakenOver] = "taken over by a planner",
        [ClarificationTextKeys.StatusSuggested] = "only suggested to the planners, not sent",
        [ClarificationTextKeys.StatusUndelivered] = "the question could not be delivered",
        [ClarificationTextKeys.StatusUnknown] = "unknown",
        [ClarificationTextKeys.MailNeutralReplySubject] = "Re: Your message to the planning team"
    };
}
