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

    public const int MaxShiftCandidates = 10;

    public const int DefaultShiftLookaheadDays = 1;

    public const int DstGapShiftHours = 1;

    public const string ReplySenderDisplayName = "Klacksy";

    public const string ReplySubjectPrefix = "Re: ";

    public const string ReplySubjectMarker = "Re:";

    public const string AutoSubmittedHeader = "Auto-Submitted";

    public const string AutoSubmittedReplyValue = "auto-replied";

    public const string InReplyToHeader = "In-Reply-To";

    public const string ReferencesHeader = "References";
}
