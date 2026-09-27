// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What the gate replays for one proposal, fixed before anything is applied so the old and the new
/// description are measured on the same items with the same model.
/// </summary>
/// <param name="ReferenceEvalRunId">The full eval run of the default goldset the default holdout items were chosen from</param>
/// <param name="Model">The model of that run, used for every replay</param>
/// <param name="ScorerVersion">Scorer version of that run</param>
/// <param name="HoldoutItems">Holdout items that involve the skill: from the default goldset's run and, when one exists, from the latest full run of the translated goldset</param>
/// <param name="TrainItems">The train misses the proposal was built from</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record GoldsetReplayPlan(
    Guid ReferenceEvalRunId,
    string Model,
    int ScorerVersion,
    IReadOnlyList<GoldsetItemRef> HoldoutItems,
    IReadOnlyList<GoldsetItemRef> TrainItems);
