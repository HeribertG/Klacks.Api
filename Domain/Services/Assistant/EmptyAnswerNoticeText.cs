// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Recognizes the two empty-answer notices of EmptyAnswerRecovery inside a stored assistant message and
/// swaps each for a short English state marker. A persisted message carries no flag that could say "this
/// sentence came from the closing guard, not from the model" - LLMMessage.Role goes verbatim to the
/// providers and LLMMessage.FunctionCalls is reserved for function-call JSON - so recognition is by text:
/// every variant GracefulCorrectionTexts resolves for either key in any language (core table plus every
/// loaded pack) and the English floor constants. Text matching also covers notices stored before this
/// class existed. A notice is replaced rather than dropped because it still states what happened: after
/// the fallback notice the steps DID run, and a summarizer that saw only the user's request would file it
/// as an open task and invite a later turn to run it again.
/// </summary>
/// <param name="content">A stored assistant message, possibly prose followed by a notice.</param>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

public static class EmptyAnswerNoticeText
{
    /// <summary>
    /// The content with every empty-answer notice replaced by its state marker; unchanged when it holds none.
    /// </summary>
    /// <param name="content">A stored assistant message.</param>
    public static string ToStateMarkers(string content)
    {
        if (string.IsNullOrEmpty(content))
        {
            return content;
        }

        var result = content;
        foreach (var (notice, marker) in Replacements())
        {
            result = result.Replace(notice, marker, StringComparison.Ordinal);
        }

        return result;
    }

    private static IEnumerable<(string Notice, string Marker)> Replacements() =>
        NoticesOf(GracefulCorrectionTexts.EmptyAnswerFallbackNotice, EmptyAnswerRecoveryConstants.FallbackNotice)
            .Select(notice => (Notice: notice, Marker: EmptyAnswerRecoveryConstants.StepsRanStateMarker))
            .Concat(NoticesOf(GracefulCorrectionTexts.EmptyAnswerNoActionNotice, EmptyAnswerRecoveryConstants.NoActionNotice)
                .Select(notice => (Notice: notice, Marker: EmptyAnswerRecoveryConstants.NothingExecutedStateMarker)))
            .OrderByDescending(pair => pair.Notice.Length);

    private static IEnumerable<string> NoticesOf(string key, string englishFloor) =>
        GracefulCorrectionTexts.AllTextsOf(key).Append(englishFloor).Distinct(StringComparer.Ordinal);
}
