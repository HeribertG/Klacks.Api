// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lifecycle statuses of a generated learning candidate, stored as text in
/// skill_learning_candidates.status. Nothing writes anything but Generated before stage G2.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningCandidateStatuses
{
    public const string Generated = "generated";
    public const string RoutingPassed = "o1_passed";
    public const string RoutingFailed = "o1_failed";
    public const string ExecutionPassed = "o2_passed";
    public const string ExecutionFailed = "o2_failed";
    public const string Active = "active";
    public const string Retired = "retired";
}
