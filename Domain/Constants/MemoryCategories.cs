// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Constants;

public static class MemoryCategories
{
    public const string Fact = "fact";
    public const string Preference = "preference";
    public const string Decision = "decision";
    public const string InteractionSummary = "interaction_summary";
    public const string UserInfo = "user_info";
    public const string ProjectContext = "project_context";
    public const string LearnedBehavior = "learned_behavior";
    public const string Correction = "correction";
    public const string Temporal = "temporal";
    public const string UserPreference = "user_preference";
    public const string SystemKnowledge = "system_knowledge";
    public const string LearnedFact = "learned_fact";
    public const string Workflow = "workflow";
    public const string Context = "context";

    /// <summary>
    /// Phase 2 autonomy: persistent user/session intent (e.g. "finish the May 2026 plan for Bern").
    /// Used by PlanningAgent to recall "where we were" across sessions.
    /// </summary>
    public const string Intent = "intent";

    /// <summary>
    /// Phase 2 autonomy: a pending task derived from an Intent, ready for the planning loop to pick up.
    /// </summary>
    public const string PendingTask = "pending_task";
}
