// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Prompt fragments of the inbound clarification dialog that extend the intent-extraction prompt of
/// InboundIntentAnalysisService: the two extra JSON fields, the rules for when a message needs a
/// question back and what that question may ask, the extra instruction for re-analysing an answered
/// question, and the user message of that re-analysis (original message, question, answer).
/// </summary>
/// <param name="senderDisplay">Sender label shown in the From line</param>
/// <param name="originalText">The employee's original, unclear message</param>
/// <param name="originalDateLine">Company-local date line of the original message</param>
/// <param name="question">The question Klacksy sent</param>
/// <param name="answerText">The employee's answer</param>
/// <param name="answerDateLine">Company-local date line of the answer</param>

using System.Globalization;

namespace Klacks.Api.Infrastructure.Inbound;

internal static class InboundClarificationPromptParts
{
    internal const string SchemaFields =
        ",\"needsClarification\":\"true|false\"," +
        "\"clarificationQuestion\":\"one short closed yes/no question in the language of the message, or null\"";

    internal const string Rules =
        " needsClarification = true only when the sender is an employee, the message concerns attendance, " +
        "absence, availability or a shift, and a planner could not act on it without asking back - for example " +
        "'I don't feel well' or 'I am sick' without saying whether and when work will be missed. Otherwise " +
        "false; a customer message is always false. clarificationQuestion: when needsClarification is true, " +
        "one short closed yes/no question in the language of the message that asks only whether and when the " +
        "sender will be absent or able to work; never ask about symptoms, illness, diagnosis or other health " +
        "details and never promise or approve anything; otherwise null.";

    internal const string AnswerInstructions =
        "\nThe user turn is a short exchange instead of a single message: the employee's original message, " +
        "one clarifying question the planning assistant sent, and the employee's answer. Classify the combined " +
        "meaning of the original message and the answer. Resolve relative dates against the Date of the " +
        "original message. If the answer still leaves attendance or the period unclear, set needsClarification " +
        "to true and clarificationQuestion to null: no further question will be sent.";

    private const string FromLabel = "From: ";
    private const string OriginalLabelFormat = "Original message (Date: {0}): ";
    private const string QuestionLabel = "Question from the planning assistant: ";
    private const string AnswerLabelFormat = "Answer (Date: {0}): ";
    private const char LineBreak = '\n';

    internal static string BuildAnswerUserMessage(
        string senderDisplay,
        string originalText,
        string originalDateLine,
        string question,
        string answerText,
        string answerDateLine)
    {
        return FromLabel + senderDisplay + LineBreak +
               string.Format(CultureInfo.InvariantCulture, OriginalLabelFormat, originalDateLine) + originalText + LineBreak +
               QuestionLabel + question + LineBreak +
               string.Format(CultureInfo.InvariantCulture, AnswerLabelFormat, answerDateLine) + answerText;
    }
}
