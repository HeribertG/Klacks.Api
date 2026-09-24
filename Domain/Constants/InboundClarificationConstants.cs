// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Tunables of the inbound clarification dialog (Klacksy asks an employee back once about an unclear
/// attendance message): answer deadline, rate limit, question guard rails, text limits and the mail
/// header names used when the question goes out as an email reply.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class InboundClarificationConstants
{
    public const int DefaultAnswerWindowMinutes = 60;

    public const int ShiftStartBufferMinutes = 30;

    public const int MinimumAnswerWindowMinutes = 10;

    public const int RateLimitWindowMinutes = 60;

    public const int MaxQuestionsPerRateLimitWindow = 1;

    public const int RecentExpiryNoteWindowHours = 24;

    public const int MaxQuestionLength = 300;

    public const int MaxQuestionSentences = 2;

    /// <summary>
    /// Max length of the raw LLM-proposed clarification question draft stored on inbound_analyses
    /// (InboundAnalysis.ClarificationQuestion / InboundAnalysisConfiguration column length). Distinct
    /// from MaxQuestionLength, which guards the shorter, sentence-trimmed question actually sent out.
    /// </summary>
    public const int MaxDraftQuestionLength = 500;

    public const int MaxOriginalTextLength = 4000;

    public const int MaxSenderDisplayLength = 300;

    public const int MaxNotifiedOriginalTextLength = 300;

    /// <summary>
    /// Column length of inbound_clarifications.shift_context; the shift name is unbounded, so the
    /// coordinator truncates the composed context to this length before saving.
    /// </summary>
    public const int MaxShiftContextLength = 200;

    /// <summary>
    /// Column length of inbound_clarifications.email_message_id; a longer original message id is not
    /// stored (truncating it would break the thread match anyway).
    /// </summary>
    public const int MaxStoredEmailMessageIdLength = 998;

    public const int MaxShiftCandidates = 10;

    public const int DefaultShiftLookaheadDays = 1;

    /// <summary>
    /// Days the shift search window reaches back before the company-local today, so a night shift that
    /// started yesterday and is still running is found for a message sent after midnight.
    /// </summary>
    public const int RunningShiftLookbackDays = 1;

    public const string ReplySubjectPrefix = "Re: ";

    public const string ReplySubjectMarker = "Re:";

    /// <summary>
    /// Fixed subject used instead of reflecting the original subject when it looks suspicious (a link, a
    /// MIME encoded-word marker, an '@' or a phone-like digit run), so none of that content is echoed
    /// back to the employee.
    /// </summary>
    public const string NeutralReplySubject = "Re: Your message to the planning team";

    public const int MaxReplySubjectLength = 120;

    /// <summary>
    /// Upper bound of the flattened original subject that is inspected for suspicious content. Twice
    /// MaxReplySubjectLength: only the first MaxReplySubjectLength characters of the reply subject are ever
    /// sent, so nothing beyond this bound can reach the employee, and the received subject is unbounded
    /// attacker-controlled text that must not reach the digit-run regexes at full length.
    /// </summary>
    public const int MaxSubjectInspectionLength = MaxReplySubjectLength * 2;

    /// <summary>
    /// Marker of a MIME encoded-word (RFC 2047), e.g. "=?UTF-8?B?...?=". A raw original subject containing
    /// it was not decoded and must not be reflected as-is.
    /// </summary>
    public const string EncodedWordMarker = "=?";

    public const int MaxMessageIdLength = 250;

    public const int MaxReferencesCount = 10;

    public const int MinSweepSeconds = 1;

    public const string AutoSubmittedHeader = "Auto-Submitted";

    public const string AutoSubmittedReplyValue = "auto-replied";

    public const string InReplyToHeader = "In-Reply-To";

    public const string ReferencesHeader = "References";
}
