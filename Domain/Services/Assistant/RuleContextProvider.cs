// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the situational scheduling rule-pack (WP-P0.2, Variant A). When any of the curated
/// scheduling skills is in scope for the turn, it injects a procedural nudge that tells the model to
/// read per-client effective limits via the read-skills, to respect the pre-commit guardrail, and to
/// leave locked/break cells untouched. It deliberately carries NO fixed limit numbers — those are
/// per-client and must be read via get_scheduling_defaults / list_scheduling_rules, mirroring the
/// always-on ontology constraint.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Models.Scheduling;

namespace Klacks.Api.Domain.Services.Assistant;

public class RuleContextProvider : IRuleContextProvider
{
    private static readonly HashSet<string> SchedulingSkillNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "find_replacement",
        "propose_plan",
        "cover_absence",
        "place_work",
        "read_schedule_state",
        "detect_conflicts",
        "interpret_resource_monitor",
        "get_scheduling_defaults",
        "list_scheduling_rules",
    };

    private const string RulePack =
        "[SCHEDULING CONTEXT]\n" +
        "This is a scheduling task. Before proposing or placing work:\n" +
        "- Call get_scheduling_defaults / list_scheduling_rules for the affected client(s) to get the EFFECTIVE limits (they differ per client; never assume fixed numbers).\n" +
        "- Every placement is pre-commit validated; respect the guardrail's blocking conflicts.\n" +
        "- Never modify locked or break cells.";

    public bool IsSchedulingContext(IReadOnlyList<string>? availableSkillNames)
        => availableSkillNames != null && availableSkillNames.Any(SchedulingSkillNames.Contains);

    public string BuildSchedulingRulePack(
        IReadOnlyList<string>? availableSkillNames,
        SchedulingPolicy? scopedClientPolicy = null)
    {
        if (!IsSchedulingContext(availableSkillNames))
        {
            return string.Empty;
        }

        if (scopedClientPolicy == null)
        {
            return RulePack;
        }

        // Additive: the procedural nudge stays (it covers the many OTHER candidate clients the planner
        // skills reason about); the concrete block is a bonus for the single client currently in view,
        // explicitly scoped so the model cannot read these numbers as global.
        return RulePack + "\n\n" + RenderScopedClientBlock(scopedClientPolicy);
    }

    private static string RenderScopedClientBlock(SchedulingPolicy policy)
    {
        var c = CultureInfo.InvariantCulture;
        return "[SELECTED-CLIENT EFFECTIVE LIMITS]\n" +
            "These limits apply ONLY to the client currently open in the UI — NOT to any other client. " +
            "For every other client you reason about (e.g. replacement candidates), you MUST still call " +
            "get_scheduling_defaults / list_scheduling_rules; these numbers do not transfer.\n" +
            $"- min rest between blocks: {policy.MinRestHours.TotalHours.ToString("0.#", c)}h\n" +
            $"- max per day: {policy.MaxDailyHours.TotalHours.ToString("0.#", c)}h\n" +
            $"- max per week: {policy.MaxWeeklyHours.TotalHours.ToString("0.#", c)}h\n" +
            $"- max consecutive days: {policy.MaxConsecutiveDays.ToString(c)}\n" +
            $"- min rest days per week: {policy.MinRestDays.ToString(c)}";
    }
}
