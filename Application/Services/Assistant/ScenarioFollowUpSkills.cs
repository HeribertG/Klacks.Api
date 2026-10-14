// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Names and rule of the scenario follow-up guarantee. A proposal scenario is produced in one turn
/// (e.g. start_autowizard) and shown, accepted or rejected in later ones, whose messages ("show me the
/// result", "yes, take it") carry no scenario vocabulary in any language. The scenario id only comes into
/// existence inside the job, and the conversation history carries message texts, not the ids of earlier tool
/// results (unless the assistant echoed them), so accept_scenario, reject_scenario and evaluate_scenario are
/// only usable when list_scenarios is in the same toolset. Only the read-only list_scenarios is ever
/// guaranteed; the mutating accept/reject skills reach the toolset through retrieval or the other guarantees
/// alone (owner decision 2026-09-30). Deliberately no previous-turn pin: any non-empty pinned list switches
/// the turn-intent classifier off for that turn, so the state-based probe is the only other signal.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant;

public static class ScenarioFollowUpSkills
{
    public const string ListScenariosSkillName = "list_scenarios";
    public const string AcceptScenarioSkillName = "accept_scenario";
    public const string RejectScenarioSkillName = "reject_scenario";
    public const string EvaluateScenarioSkillName = "evaluate_scenario";

    private static readonly HashSet<string> ScenarioIdConsumerSkillNames = new(StringComparer.OrdinalIgnoreCase)
    {
        AcceptScenarioSkillName,
        RejectScenarioSkillName,
        EvaluateScenarioSkillName
    };

    public static bool NeedsScenarioList(IEnumerable<string> selectedSkillNames) =>
        selectedSkillNames.Any(ScenarioIdConsumerSkillNames.Contains);
}
