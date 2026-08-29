// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The only part of the learning loop that talks to a language model. Provider-agnostic by construction:
/// it asks whichever model the installation enabled through the shared cheapest-model resolver, and it
/// never sees more of an utterance than the stored excerpt.
/// </summary>
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Interfaces.Assistant;

public interface ILearnedArtifactGenerator
{
    /// <summary>
    /// Classifies every cluster of one run in a single call. Batched on purpose: a run handles a handful
    /// of clusters, and one request per cluster would multiply the cost of the run for no better answer.
    /// Returns an empty list when no model is configured or the answer could not be parsed.
    /// </summary>
    Task<IReadOnlyList<SkillLearningClassification>> ClassifyAsync(
        IReadOnlyList<SkillLearningTriageInput> inputs,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Produces trigger phrase variants in the language of the cluster for the target skill.
    /// </summary>
    /// <param name="failureHint">What the previous round produced and why it failed, empty on the first round</param>
    Task<IReadOnlyList<string>> GeneratePhrasesAsync(
        SkillLearningClusterContext cluster,
        string targetSkill,
        string targetDescription,
        IReadOnlyList<string> existingPhrases,
        string? failureHint,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Produces capability variants: recipes that chain existing skills to serve the cluster's wish.
    /// The model may only use the building blocks it is given - the caller has already reduced them to
    /// the skills that are both relevant to this wish and safe to compose.
    /// </summary>
    /// <param name="blocks">Skills the variants may be built from, with their parameters</param>
    /// <param name="examples">Existing recipes, serialised, as the format the answer must follow</param>
    /// <param name="failureHint">Why the previous round's variants were rejected, empty on the first round</param>
    Task<IReadOnlyList<LearnedRecipeDraft>> GenerateCapabilitiesAsync(
        SkillLearningClusterContext cluster,
        IReadOnlyList<CapabilityBuildingBlock> blocks,
        IReadOnlyList<string> examples,
        string? failureHint,
        CancellationToken cancellationToken = default);
}
