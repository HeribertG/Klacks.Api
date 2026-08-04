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

    /// <summary>
    /// A lesson the assistant drew from a turn that demonstrably went wrong: a failed skill call or a
    /// user correction. Deliberately its own category and never mixed into the factual pool, because a
    /// reflection is an experience that may be wrong, not a fact that was established.
    /// </summary>
    public const string Reflection = "reflection";
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

    /// <summary>
    /// Companion: a learned personal interest of a specific user (e.g. a sport they follow),
    /// captured by the curiosity engine and used to make small talk feel personal.
    /// </summary>
    public const string InterestProfile = "interest_profile";

    /// <summary>
    /// Importance floor applied to durable company-wide invariants that are auto-pinned,
    /// so they outrank transient pinned memories within the per-turn pinned-memory cap.
    /// </summary>
    public const int CompanyInvariantPinnedImportance = 8;

    private static readonly HashSet<string> PersonalCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        Preference,
        UserPreference,
        UserInfo,
        InterestProfile
    };

    private static readonly HashSet<string> CompanyInvariantCategories = new(StringComparer.OrdinalIgnoreCase)
    {
        Fact,
        LearnedFact,
        SystemKnowledge
    };

    /// <summary>
    /// True for categories that belong to one specific user (and must be scoped by UserId),
    /// as opposed to shared company-wide knowledge.
    /// </summary>
    public static bool IsPersonal(string? category) =>
        !string.IsNullOrWhiteSpace(category) && PersonalCategories.Contains(category);

    /// <summary>
    /// True for durable, company-wide truths (e.g. a naming convention) that should stay
    /// available in every turn; such memories are auto-pinned and importance-boosted.
    /// </summary>
    public static bool IsCompanyInvariant(string? category) =>
        !string.IsNullOrWhiteSpace(category) && CompanyInvariantCategories.Contains(category);
}
