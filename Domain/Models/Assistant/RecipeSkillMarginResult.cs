// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Outcome of the shadow-mode margin computation. Pure diagnostics — never consumed by routing. Margin
/// is null when no comparable pair exists (no foreign skill candidate, or the served skill could not be
/// scored). WouldGateAtPlaceholderThreshold gates on margin below the placeholder epsilon.
/// </summary>
/// <param name="BestServedSkill">Highest-scoring served skill, or null when none was scored.</param>
/// <param name="BestServedScore">Reranker score of BestServedSkill, or null.</param>
/// <param name="BestForeignSkill">Highest-scoring non-served skill, or null when none competed.</param>
/// <param name="BestForeignScore">Reranker score of BestForeignSkill, or null.</param>
/// <param name="Margin">BestServedScore minus BestForeignScore, or null when not computable.</param>
/// <param name="WouldGateAtPlaceholderThreshold">True when Margin is below the placeholder epsilon.</param>
/// <param name="PermissionScope">Which permission scope the KNN ran under (for calibration honesty).</param>
/// <param name="ServedSkillsNotScored">Count of served skills that had no comparable score.</param>
/// <param name="OldDetectorDecision">The unchanged CompetingSkillIntentDetector verdict for divergence.</param>
public sealed record RecipeSkillMarginResult(
    string? BestServedSkill,
    double? BestServedScore,
    string? BestForeignSkill,
    double? BestForeignScore,
    double? Margin,
    bool WouldGateAtPlaceholderThreshold,
    string PermissionScope,
    int ServedSkillsNotScored,
    bool OldDetectorDecision);
