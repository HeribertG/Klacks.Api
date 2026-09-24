// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Texts of the closing guard that replaces an empty or stand-in-only answer: the instruction of the
/// single tool-less follow-up call - one for a turn that ran tools, one for a turn that ran none - and
/// the last-resort English notices used when
/// that call fails or again produces no answer AND GracefulCorrectionTexts has nothing to localize them
/// with: FallbackNotice after a turn that ran tools, NoActionNotice after a turn that ran none, where
/// claiming "I ran the requested steps" would be untrue. In every language Klacks ships,
/// GracefulCorrectionTexts.EmptyAnswerFallbackNotice and EmptyAnswerNoActionNotice resolve instead, so
/// these constants are a defensive floor rather than the text users normally see. On the
/// streaming path the recovered answer is appended below text that is already on screen, set apart by
/// AppendedAnswerSeparator. Conversation compaction replaces a stored notice with the matching state
/// marker (StepsRanStateMarker, NothingExecutedStateMarker), so the summarizer keeps what happened without
/// reading the canned sentence.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class EmptyAnswerRecoveryConstants
{
    public const string RecoveryInstruction =
        "Answer the user now in their language, based only on the tool results in this conversation. " +
        "Do not call tools and do not output bracketed status markers or tool-call notes.";

    public const string ToolLessRecoveryInstruction =
        "Answer the user's last message now in their language. " +
        "Nothing was executed in this turn, so do not state that any change was made or any action was done. " +
        "Do not call tools and do not output bracketed status markers.";

    public const string FallbackNotice =
        "I ran the requested steps but could not formulate an answer. Please ask again.";

    public const string NoActionNotice =
        "I could not produce an answer and nothing was executed. Please try again.";

    public const string AppendedAnswerSeparator = "\n\n";

    public const string StepsRanStateMarker = "(Steps were executed, but no answer text was produced.)";

    public const string NothingExecutedStateMarker = "(No answer was produced and nothing was executed.)";
}
