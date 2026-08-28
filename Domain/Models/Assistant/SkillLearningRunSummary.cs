// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// What one learning run did, for the run log.
/// </summary>
/// <param name="AlreadyRouted">Clusters whose target was retrievable before anything was learned</param>
/// <param name="Blocked">Description proposals withheld because the regression gate turned red</param>
namespace Klacks.Api.Domain.Models.Assistant;

public sealed record SkillLearningRunSummary(
    int Processed,
    int Learned,
    int AlreadyRouted,
    int Unfulfillable,
    int Failed,
    int Sharpened,
    int Blocked)
{
    public static SkillLearningRunSummary Empty { get; } = new(0, 0, 0, 0, 0, 0, 0);
}
