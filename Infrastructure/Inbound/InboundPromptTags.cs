// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The untrusted-data tag pairs used by the inbound prompts. Text written by the sender (or derived from
/// it by an earlier model step) is wrapped in one of these pairs by UntrustedTextBlock, and the matching
/// system prompt names the same tags, so the constants live here once instead of being repeated in the
/// intent analysis, the answer analysis and the clarification question composer.
/// </summary>

namespace Klacks.Api.Infrastructure.Inbound;

internal static class InboundPromptTags
{
    internal const string SenderOpen = "<sender>";
    internal const string SenderClose = "</sender>";
    internal const string SubjectOpen = "<subject>";
    internal const string SubjectClose = "</subject>";
    internal const string EmployeeMessageOpen = "<employee_message>";
    internal const string EmployeeMessageClose = "</employee_message>";
    internal const string DraftQuestionOpen = "<draft_question>";
    internal const string DraftQuestionClose = "</draft_question>";
    internal const string OriginalMessageOpen = "<original_message>";
    internal const string OriginalMessageClose = "</original_message>";
    internal const string SentQuestionOpen = "<sent_question>";
    internal const string SentQuestionClose = "</sent_question>";
    internal const string EmployeeAnswerOpen = "<employee_answer>";
    internal const string EmployeeAnswerClose = "</employee_answer>";
}
