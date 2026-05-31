// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Produces a situational rule-pack block for the LLM context when the current turn is a scheduling
/// task. The block is a procedural nudge (call the read-skills for per-client effective limits,
/// respect the guardrail, never touch locked/break cells), not a dump of fixed default numbers — it
/// reinforces the always-on ontology rule "never assume fixed numbers".
/// </summary>
public interface IRuleContextProvider
{
    /// <summary>
    /// Returns the scheduling rule-pack block when at least one of the curated scheduling skills is in
    /// scope for the turn, otherwise an empty string.
    /// </summary>
    /// <param name="availableSkillNames">The skill names offered to the LLM this turn.</param>
    string BuildSchedulingRulePack(IReadOnlyList<string>? availableSkillNames);
}
