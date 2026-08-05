// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Strips assistant-response fragments that are not the model's own factual prose before claim
/// extraction: the appended no-action / recipe-failure notices and the [SUGGESTIONS]/[REPLIES]
/// UI blocks. Everything after the recipe-failure prefix is cut because it is server-authored.
/// </summary>

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant.Grounding;

public static class AnswerGroundingResponseSanitizer
{
    public static string Sanitize(string responseContent)
    {
        if (string.IsNullOrEmpty(responseContent))
        {
            return string.Empty;
        }

        var text = responseContent.Replace(MutationGuardConstants.NoActionStreamNotice, string.Empty, StringComparison.Ordinal);

        var cut = text.IndexOf(MutationGuardConstants.RecipeStepFailedNoticePrefix, StringComparison.Ordinal);
        if (cut >= 0)
        {
            text = text[..cut];
        }

        text = LLMResponseBuilder.SuggestionsBlockRegex.Replace(text, string.Empty);
        text = LLMResponseBuilder.RepliesBlockRegex.Replace(text, string.Empty);

        return text;
    }
}
