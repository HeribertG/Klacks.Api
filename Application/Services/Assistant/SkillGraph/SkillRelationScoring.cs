// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Shared scoring helper for skill-relationship edges: the per-type active threshold and a confidence
/// adjustment that clamps to the valid range and recomputes the edge status (retired/active/candidate).
/// Used by the human accept/dismiss commands.
/// </summary>

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Application.Services.Assistant.SkillGraph;

public static class SkillRelationScoring
{
    public static double ActiveThreshold(SkillRelationType type)
        => type == SkillRelationType.Sequential
            ? SkillGraphConstants.SequentialActiveThreshold
            : SkillGraphConstants.CoRequiredActiveThreshold;

    public static void Adjust(SkillRelation edge, double delta)
    {
        edge.Confidence = Math.Clamp(edge.Confidence + delta, 0, SkillGraphConstants.MaxLearnedConfidence);
        edge.Status = edge.Confidence <= SkillGraphConstants.RetireConfidence
            ? SkillRelationStatus.Retired
            : edge.Confidence >= ActiveThreshold(edge.Type)
                ? SkillRelationStatus.Active
                : SkillRelationStatus.Candidate;
    }
}
