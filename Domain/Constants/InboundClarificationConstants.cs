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

    public const int MaxSweepSeconds = 86_400;

    /// <summary>
    /// Days after which the raw original message text of an ended clarification round is cleared: counted
    /// from resolved_at for Answered, Expired, TakenOver and Unresolved, and from asked_at for Suggested
    /// (which never gets a resolved_at). Open rounds are never cleared by this rule.
    /// </summary>
    public const int DefaultOriginalTextRetentionDays = 30;

    /// <summary>
    /// Lower bound of the configured retention: a zero or negative value would wipe the text of a round
    /// the moment it closes, so it is raised to one day.
    /// </summary>
    public const int MinOriginalTextRetentionDays = 1;

    /// <summary>
    /// Upper bound of the configured retention (ten years); also keeps the cutoff calculation inside the
    /// range of DateTime for absurd configured values.
    /// </summary>
    public const int MaxOriginalTextRetentionDays = 3650;

    /// <summary>
    /// Column length of inbound_clarifications.recipient: the longest valid email address (RFC 5321). A
    /// reply target longer than this is treated as no personal target, because the insert would fail.
    /// </summary>
    public const int MaxRecipientLength = 254;

    public const string AutoSubmittedHeader = "Auto-Submitted";

    public const string AutoSubmittedReplyValue = "auto-replied";

    public const string InReplyToHeader = "In-Reply-To";

    public const string ReferencesHeader = "References";
}
