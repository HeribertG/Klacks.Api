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
    /// Replays the stored golden cases and returns the ones that no longer route. A case that was
    /// already failing before the change is not a regression, which is why the caller passes the
    /// baseline it measured before activating anything.
    /// </summary>
    Task<IReadOnlyList<string>> FindFailingGoldenCasesAsync(
        IReadOnlyList<SkillLearningGoldenCase> goldenCases, CancellationToken cancellationToken = default);
}
