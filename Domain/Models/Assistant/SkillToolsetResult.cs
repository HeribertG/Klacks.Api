// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

public class SkillToolsetResult
{
    public List<LLMFunction> Functions { get; init; } = new();

    public bool HasDomainSkillContext { get; init; }

    /// <summary>
    /// Wall-clock milliseconds toolset assembly took for this turn (retrieval being the dominant
    /// part). Carried into the turn's usage row so per-installation diagnostics can separate
    /// pipeline time from model time regardless of which LLM the customer selected.
    /// </summary>
    public long AssemblyMs { get; init; }

    /// <summary>
    /// True when the turn-intent classifier read the message as a confident question although the mutation
    /// detector matched a state-changing word: the chat loop then does not force a tool call for that reason.
    /// </summary>
    public bool TurnIntentVetoesForcedToolCall { get; init; }

    /// <summary>
    /// How this turn's skill retrieval was ordered (margin, reranked, explored); null when no skill pass ran. Carried
    /// into the trajectory for the skill rerank threshold tuner.
    /// </summary>
    public SkillRerankObservation? SkillRerank { get; init; }

    /// <summary>
    /// Where skills entered or left the selection this turn; observation only, never an input to the assembly. Null
    /// on every path that does not build a capped or always-on-only selection.
    /// </summary>
    public SkillToolsetTrace? Trace { get; init; }
}
