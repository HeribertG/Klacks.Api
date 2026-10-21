// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Composes the volatile (per-turn, uncached) system-prompt segment of a chat turn and appends per-turn and
/// per-iteration notes to it, so a note that changes every turn never touches the provider-cached stable segment.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant;

internal static class VolatilePromptComposer
{
    private const string NoteSeparator = "\n\n";

    /// <summary>
    /// The turn's volatile system-prompt segment: temporal context, the context-derived additions, the
    /// soul/memory segment, - on a correction turn - LLMContext.CorrectionNote and - on the turn after an accepted
    /// explain offer - LLMContext.ExplainOfferNote. The correction note
    /// belongs HERE rather than in the per-iteration note slot of the loop: the pending/recipe/plan notes
    /// are alternatives to each other, while a correction note applies to every iteration of the turn it
    /// belongs to. Volatile rather than stable so a note that changes every turn cannot invalidate a
    /// provider's cached stable segment.
    /// </summary>
    /// <param name="temporalContext">The turn's date/time block.</param>
    /// <param name="context">The turn context, source of the volatile additions and the correction note.</param>
    /// <param name="soulAndMemoryVolatilePrompt">Volatile half of the soul/memory assembly, null when it did not run.</param>
    internal static string BuildVolatilePrompt(string? temporalContext, LLMContext context, string? soulAndMemoryVolatilePrompt)
    {
        return CombineVolatile(
            temporalContext,
            CombineVolatile(
                CombineVolatile(
                    CombineVolatile(LLMSystemPromptBuilder.BuildVolatileAdditions(context), soulAndMemoryVolatilePrompt),
                    context.CorrectionNote),
                context.ExplainOfferNote));
    }

    // Appends a per-turn instruction note (confirmation gate, ask-step, forced recipe, plan nudge) to the
    // volatile system-prompt segment instead of the stable one, so a note that changes every turn can
    // never invalidate a provider's cached stable segment (e.g. Anthropic prompt caching).
    internal static string CombineVolatile(string? basePrompt, string? note)
    {
        if (string.IsNullOrEmpty(note))
        {
            return basePrompt ?? string.Empty;
        }

        return string.IsNullOrEmpty(basePrompt) ? note : $"{basePrompt}{NoteSeparator}{note}";
    }
}
