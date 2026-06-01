// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Interfaces.Assistant;

/// <summary>
/// Produces a situational rule-pack block for the LLM context when the current turn is a scheduling
/// task. The block is a procedural nudge (call the read-skills for per-client effective limits,
/// respect the guardrail, never touch locked/break cells), not a dump of fixed default numbers — it
/// reinforces the always-on ontology rule "never assume fixed numbers". When a single client is in
/// scope, the resolved effective limits of THAT client are appended as a tightly-scoped block (added
/// to, never replacing, the procedural nudge).
/// </summary>
public interface IRuleContextProvider
{
    /// <summary>
    /// True when at least one of the curated scheduling skills is offered to the LLM this turn.
    /// </summary>
    /// <param name="availableSkillNames">The skill names offered to the LLM this turn.</param>
    bool IsSchedulingContext(IReadOnlyList<string>? availableSkillNames);

    /// <summary>
    /// Returns the scheduling rule-pack block when the turn is a scheduling context, otherwise an empty
    /// string. When <paramref name="scopedClientPolicy"/> is supplied, a tightly-scoped per-client
    /// effective-limits block is appended to the procedural nudge.
    /// </summary>
    /// <param name="availableSkillNames">The skill names offered to the LLM this turn.</param>
    /// <param name="scopedClientPolicy">Effective limits of the single in-scope client, or null.</param>
    string BuildSchedulingRulePack(
        IReadOnlyList<string>? availableSkillNames,
        SchedulingPolicy? scopedClientPolicy = null);
}
