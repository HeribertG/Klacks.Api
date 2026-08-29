// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Oracle O1: answers "would this utterance reach that skill right now" against the live retrieval
/// pipeline, without a language model and without running anything. It is the only judge of whether a
/// learned phrase helped, and the gate that stops one learned artefact from breaking another.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ISkillRoutingOracle
{
    /// <summary>
    /// Assembles the toolset the given utterance would produce and reports whether the target is in it.
    /// Runs with administrator rights so a permission the triggering user lacks cannot be mistaken for a
    /// routing gap. An empty target is allowed: the probe then only reports what the assembler offered,
    /// which is what the classifier needs before any target is known.
    /// </summary>
    Task<SkillRoutingProbe> ProbeAsync(
        string utterance, string? locale, string targetSkill, CancellationToken cancellationToken = default);

    /// <summary>
    /// Skill names the retrieval stage can still reach for this utterance - a strictly wider set than the
    /// one ProbeAsync assembles. The assembler takes only KnowledgeIndexConstants.DefaultTopK of the
    /// reranked pool, so a skill below that rank never reaches a toolset however relevant it is, and those
    /// are exactly the skills a routing gap is made of. The classifier has to be allowed to name them;
    /// confined to what is already offered it can only ever pick something that needs no phrase.
    /// Bounded by the reranker pool (MaxRerankerCandidates), so this can never widen into "the catalogue",
    /// and every name in it is a real, permitted, indexed skill.
    /// </summary>
    /// <param name="utterance">The stored wish excerpt to retrieve for</param>
    Task<IReadOnlyList<string>> ListReachableSkillsAsync(
        string utterance, CancellationToken cancellationToken = default);

    /// <summary>
    /// Replays the stored golden cases and returns the ones that no longer route. A case that was
    /// already failing before the change is not a regression, which is why the caller passes the
    /// baseline it measured before activating anything.
    /// </summary>
    Task<IReadOnlyList<string>> FindFailingGoldenCasesAsync(
        IReadOnlyList<SkillLearningGoldenCase> goldenCases, CancellationToken cancellationToken = default);
}
