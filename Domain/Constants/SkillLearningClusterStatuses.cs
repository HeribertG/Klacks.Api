// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lifecycle statuses of a skill learning cluster, stored as text in skill_learning_clusters.status.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningClusterStatuses
{
    public const string Collecting = "collecting";
    public const string Ready = "ready";
    public const string Learning = "learning";
    public const string LearnedPhrase = "learned_phrase";
    public const string LearnedCapability = "learned_capability";
    public const string Unfulfillable = "unfulfillable";
    public const string Dismissed = "dismissed";
    public const string Retired = "retired";
}
