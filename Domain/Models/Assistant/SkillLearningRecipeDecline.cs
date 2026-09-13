// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A recipe the user turned down at its confirmation question. Carries the cluster key and excerpt of the
/// utterance that TRIGGERED the recipe — not of the negation that declined it — because the evidence being
/// collected is "this wording must stop matching this recipe".
/// </summary>
/// <param name="ClusterKey">MessageNormalizer hash of the trigger utterance, taken from its trajectory</param>
/// <param name="IntentExcerpt">Stored excerpt of the trigger utterance, at most 120 characters</param>
/// <param name="RecipeName">Recipe whose confirmation gate was declined, stored as the case's chosen artefact</param>
/// <param name="ToolsetJson">Names of the tools that were offered for the triggering turn</param>
/// <param name="TrajectoryId">Trajectory of the triggering turn, the one just marked as declined</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningRecipeDecline(
    Guid AgentId,
    string ClusterKey,
    string IntentExcerpt,
    string? UserId,
    string? Locale,
    string? RecipeName,
    string ToolsetJson,
    Guid TrajectoryId);
