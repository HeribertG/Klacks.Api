// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// All planner-facing texts of the inbound clarification dialog: question started, answer context
/// (answered or still unclear), question unanswered (expired), send failure, suggested question at
/// global level Propose or with the kill switch, missing personal contact, and a message that arrived
/// after its question had expired. Times are company-local. JoinContext combines two optional context
/// blocks for the regular analysis notification. SendFailed never repeats the raw provider/exception
/// text to planners — that detail belongs in the logs only (ClarificationCoordinator logs ids and the
/// exception or error string); the planner-facing text stays generic on purpose.
/// </summary>
/// <param name="sender">Sender label shown to planners</param>
/// <param name="question">The clarification question</param>
/// <param name="askedLocal">Company-local time the question was asked</param>
/// <param name="deadlineLocal">Company-local answer deadline</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Infrastructure.Inbound;

internal static class ClarificationNotificationTexts
{
    private const string LocalTimeFormat = "yyyy-MM-dd HH:mm";
    private const string ContextSeparator = "\n\n";
    private const string NoShiftKnown = "none found in the plan";
    private const string Ellipsis = "…";

    internal static string Started(string sender, string summary, string question, string? shiftContext, DateTime deadlineLocal) =>
        $"💬 **Clarification requested** — {sender}\n" +
        $"The message was unclear: {summary}\n" +
        $"Klacksy asked back privately: \"{question}\"\n" +
        $"Affected shift: {shiftContext ?? NoShiftKnown}\n" +
        $"Answer expected by {Format(deadlineLocal)}. You will be informed about the answer, or when none arrives in time.";

    internal static string AnswerContext(string question, DateTime askedLocal, string originalText, bool unresolved)
    {
        var context =
            $"💬 Answer to Klacksy's question \"{question}\" (asked {Format(askedLocal)}).\n" +
            $"Original message: {Shorten(originalText)}";

        return unresolved
            ? context + "\n⚠️ The answer is still unclear. Klacksy does not ask a second time, please follow up personally."
            : context;
    }

    internal static string Expired(
        string sender, string question, DateTime askedLocal, DateTime deadlineLocal, string originalText, string? shiftContext) =>
        $"⏰ **Question unanswered** — {sender}\n" +
        $"Klacksy asked \"{question}\" at {Format(askedLocal)}; there was no answer by {Format(deadlineLocal)}.\n" +
        $"Original message: {Shorten(originalText)}\n" +
        $"Affected shift: {shiftContext ?? NoShiftKnown}\n" +
        "Please follow up personally.";

    internal static string SendFailed(string question) =>
        $"⚠️ Klacksy's question \"{question}\" could not be sent. Please follow up personally.";

    internal static string Suggested(string question) =>
        $"💡 Klacksy would ask back: \"{question}\" — questions are only sent automatically from the global autonomy " +
        "level Assisted upwards and while the kill switch is off.";

    internal static string NoPersonalTarget() =>
        "ℹ️ The message is unclear, but Klacksy cannot ask back: no unambiguous personal contact could be determined " +
        "for this employee on this channel.";

    internal static string AnsweredAfterExpiry(string question, DateTime askedLocal) =>
        $"ℹ️ This message arrived after Klacksy's question \"{question}\" (asked {Format(askedLocal)}) had expired unanswered.";

    internal static string ArrivedAfterClosure(string question, DateTime askedLocal, string statusText) =>
        $"ℹ️ This message arrived after Klacksy's question \"{question}\" (asked {Format(askedLocal)}) had already been closed: {statusText}.";

    internal static string? JoinContext(string? first, string? second)
    {
        if (string.IsNullOrWhiteSpace(first))
        {
            return string.IsNullOrWhiteSpace(second) ? null : second;
        }

        return string.IsNullOrWhiteSpace(second) ? first : first + ContextSeparator + second;
    }

    private static string Format(DateTime local) => local.ToString(LocalTimeFormat, CultureInfo.InvariantCulture);

    private static string Shorten(string text) =>
        text.Length <= InboundClarificationConstants.MaxNotifiedOriginalTextLength
            ? text
            : text[..InboundClarificationConstants.MaxNotifiedOriginalTextLength] + Ellipsis;
}
