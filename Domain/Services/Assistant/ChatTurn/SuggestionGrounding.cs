// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Grounds the suggestion chips of a finished chat turn against the real entity names of the recipe slot
/// the turn asked for, so an invented name never reaches the user. Shared by both chat paths.
/// </summary>
/// <param name="suggestionEntityNameReader">Reads the real entity names for a recipe slot</param>

using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Services.Assistant.ChatTurn;

internal sealed class SuggestionGrounding
{
    private readonly ISuggestionEntityNameReader _suggestionEntityNameReader;

    internal SuggestionGrounding(ISuggestionEntityNameReader suggestionEntityNameReader)
    {
        _suggestionEntityNameReader = suggestionEntityNameReader;
    }

    /// <summary>
    /// Drops any suggestion chip that does not match a real entity name for the given recipe slot.
    /// The LLM's [SUGGESTIONS: ...] block is parsed from free text with no grounding (LLMResponseBuilder),
    /// so a plausible-sounding but non-existent contract/group name can otherwise reach the user as a
    /// toast before the (hard, DB-backed) skill resolution step ever runs. No-ops when the slot is null
    /// or not one ISuggestionEntityNameReader knows how to ground.
    /// </summary>
    internal async Task ApplyAsync(LLMResponse response, string? slot, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(slot) || response.Suggestions is not { Count: > 0 })
        {
            return;
        }

        var realNames = await _suggestionEntityNameReader.GetRealNamesForSlotAsync(slot, cancellationToken);
        if (realNames == null)
        {
            return;
        }

        response.Suggestions = SuggestionGroundingFilter.Filter(response.Suggestions, realNames);
    }
}
