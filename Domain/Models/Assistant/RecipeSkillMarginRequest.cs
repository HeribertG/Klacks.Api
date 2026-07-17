// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Input for the shadow-mode recipe-vs-skill margin evaluation. Carries the RAW user message (not the
/// history-rewritten retrieval query), the keyword-matched recipe and the skills it serves, plus the
/// legacy detector's verdict so the shadow log can record the divergence between the two signals.
/// </summary>
/// <param name="Message">The raw user chat message the recipe keyword-trigger matched.</param>
/// <param name="RecipeName">Name of the recipe whose keyword trigger fired.</param>
/// <param name="MatchedTrigger">Human-readable description of the matched trigger (log field only).</param>
/// <param name="ServedSkillNames">Skill names the recipe executes itself; force-included as candidates.</param>
/// <param name="OldDetectorDecision">What CompetingSkillIntentDetector decided (true = it would gate).</param>
/// <param name="OldDetectorCompetingSkills">Foreign skill names the legacy detector flagged, if any.</param>
/// <param name="UserRights">The caller's permissions; null when not reachable at the seam (then logged).</param>
public sealed record RecipeSkillMarginRequest(
    string Message,
    string RecipeName,
    string MatchedTrigger,
    IReadOnlyCollection<string> ServedSkillNames,
    bool OldDetectorDecision,
    IReadOnlyCollection<string> OldDetectorCompetingSkills,
    IReadOnlyCollection<string>? UserRights);
