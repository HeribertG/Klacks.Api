// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Lifecycle statuses of a generated learning candidate, stored as text in
/// skill_learning_candidates.status. Nothing writes anything but Generated before stage G2.
/// </summary>
namespace Klacks.Api.Domain.Constants;

public static class SkillLearningCandidateStatuses
{
    public const string Generated = "generated";

    /// <summary>
    /// A capability the draft validator refused before either oracle saw it - a trigger that would
    /// collide with an existing recipe or swallow a skill's own phrase. Deliberately distinct from
    /// RoutingFailed: both used to be recorded as "o1_failed", which made a rejected trigger and a
    /// phrase that did not reach its skill indistinguishable in the candidate table, and sent the first
    /// live diagnosis of this looking for an oracle that had never run.
    /// </summary>
    public const string ValidationFailed = "validation_failed";

    public const string RoutingPassed = "o1_passed";
    public const string RoutingFailed = "o1_failed";
    public const string ExecutionPassed = "o2_passed";
    public const string ExecutionFailed = "o2_failed";
    public const string Active = "active";
    public const string Retired = "retired";
}
