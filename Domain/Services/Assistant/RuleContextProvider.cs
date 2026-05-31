// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds the situational scheduling rule-pack (WP-P0.2, Variant A). When any of the curated
/// scheduling skills is in scope for the turn, it injects a procedural nudge that tells the model to
/// read per-client effective limits via the read-skills, to respect the pre-commit guardrail, and to
/// leave locked/break cells untouched. It deliberately carries NO fixed limit numbers — those are
/// per-client and must be read via get_scheduling_defaults / list_scheduling_rules, mirroring the
/// always-on ontology constraint.
/// </summary>

using Klacks.Api.Domain.Interfaces.Assistant;

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

    public string BuildSchedulingRulePack(IReadOnlyList<string>? availableSkillNames)
    {
        if (availableSkillNames == null || availableSkillNames.Count == 0)
        {
            return string.Empty;
        }

        var isSchedulingContext = availableSkillNames.Any(SchedulingSkillNames.Contains);
        return isSchedulingContext ? RulePack : string.Empty;
    }
}
