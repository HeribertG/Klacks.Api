// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Per-turn hand-over between the find_tools skill and the chat loop: the skill offers the skills it found, and the
/// function executor takes them after the tool round and adds them to the turn's toolset, so they are callable from
/// the next iteration on. Keyed by turn, because the Slack bridge and jobs reuse one DI scope across many turns.
/// </summary>
public interface ITurnToolsetExtension
{
    /// <summary>
    /// Remembers the functions for the turn and returns the ones accepted: each name once per turn, and no more than
    /// ToolSearchConstants.MaxAddedToolsPerTurn names per turn. Offering for a new turn forgets the previous one.
    /// </summary>
    /// <param name="turnId">The turn the functions were found in</param>
    /// <param name="functions">The found functions, best first</param>
    IReadOnlyList<LLMFunction> Offer(Guid? turnId, IReadOnlyList<LLMFunction> functions);

    /// <summary>
    /// Returns the functions offered for the turn since the last call, each once; empty for any other turn.
    /// </summary>
    /// <param name="turnId">The turn whose offered functions are taken</param>
    IReadOnlyList<LLMFunction> TakeOffered(Guid? turnId);
}
