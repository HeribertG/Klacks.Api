// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Texts of the closing guard that replaces an empty or stand-in-only answer after a turn that ran tools:
/// the instruction of the single tool-less follow-up call, and the last-resort English notice used when
/// that call fails or again produces no answer AND GracefulCorrectionTexts has nothing to localize it
/// with. In every language Klacks ships, GracefulCorrectionTexts.EmptyAnswerFallbackNotice resolves
/// instead, so this constant is a defensive floor rather than the text users normally see. On the
/// streaming path the recovered answer is appended below text that is already on screen, set apart by
/// AppendedAnswerSeparator.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class EmptyAnswerRecoveryConstants
{
    public const string RecoveryInstruction =
        "Answer the user now in their language, based only on the tool results in this conversation. " +
        "Do not call tools and do not output bracketed status markers or tool-call notes.";

    public const string FallbackNotice =
        "I ran the requested steps but could not formulate an answer. Please ask again.";

    public const string AppendedAnswerSeparator = "\n\n";
}
