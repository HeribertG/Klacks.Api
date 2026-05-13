// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Phase 3 (autonomy roadmap) — read-only Klacks domain ontology.
/// Exposes "what entities exist, how do they relate, which constraints apply" so the
/// planning agent and skill-selection pipeline can reason about cross-entity dependencies
/// (e.g. Work.LockLevel ⇒ immutable for wizards) instead of relying on isolated memories.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface IKlacksOntologyService
{
    /// <summary>
    /// Returns the names of all top-level domain entities (Client, Shift, Work, Break, Contract, ...).
    /// </summary>
    IReadOnlyList<string> GetEntities();

    /// <summary>
    /// Returns named relations referencing a given entity (e.g. Client ←hasMany→ Work, hasMany→ Contract).
    /// </summary>
    IReadOnlyList<KlacksOntologyRelation> GetRelations(string entityName);

    /// <summary>
    /// Returns hard constraints attached to a given entity that the planner must respect
    /// (e.g. "Work with lock_level &gt; 0 is immutable for wizards").
    /// </summary>
    IReadOnlyList<string> GetConstraints(string entityName);

    /// <summary>
    /// Compact textual ontology block injected into the context-assembly system-prompt.
    /// </summary>
    string RenderWorldModelBlock(int maxTokens = 1500);
}

public record KlacksOntologyRelation(string From, string To, string Kind);
