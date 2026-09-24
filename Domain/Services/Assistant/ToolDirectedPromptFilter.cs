// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Removes the tool-directed instructions from a volatile system-prompt segment before it goes into a
/// request that carries no tools. Such a request (recipe confirmation or ask step, empty-answer recovery)
/// cannot call anything, and a "call tool X" line makes a reasoning model deliberate about the tool it
/// lacks instead of answering. Currently that is the pending-notes hint; its line is recognized by the
/// same marker ContextAssemblyPipeline writes it with, so the two cannot drift apart.
/// </summary>
using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class ToolDirectedPromptFilter
{
    private const char LineBreak = '\n';

    /// <summary>
    /// The segment without its tool-directed lines; null and empty input are returned unchanged.
    /// </summary>
    /// <param name="volatileSystemPrompt">The combined volatile segment of a tool-less request.</param>
    internal static string? ForToolLessRequest(string? volatileSystemPrompt)
    {
        if (string.IsNullOrEmpty(volatileSystemPrompt)
            || !volatileSystemPrompt.Contains(PendingNotesPromptConstants.Marker, StringComparison.Ordinal))
        {
            return volatileSystemPrompt;
        }

        var kept = volatileSystemPrompt
            .Split(LineBreak)
            .Where(line => !line.TrimStart().StartsWith(PendingNotesPromptConstants.Marker, StringComparison.Ordinal));
        return string.Join(LineBreak, kept);
    }
}
