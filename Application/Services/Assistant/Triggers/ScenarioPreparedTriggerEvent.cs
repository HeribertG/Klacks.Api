// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Fired once per planner when Klacksy has laid a remediation scenario ready for a finding.
///
/// Addressed per recipient (TargetUserId) rather than broadcast with PlannersOnly, and that is not a
/// stylistic choice: AgentConditionLedgerPolicy.IsLedgerTracked reads a PlannersOnly event without a
/// target as one that becomes a condition of its own. No detector ever reports this kind, so such a row
/// would never appear in a tick's fingerprint set, MarkResolvedAsync would never close it, and a
/// phantom finding would sit in the digest and the findings list for good. The preparation service
/// therefore resolves the planning audience itself and raises one targeted event each. The dispatch
/// rows consequently carry no ConditionId, which is right as well - dismissing "your scenario is ready"
/// means "stop showing me this note", not "I reject the finding"; rejecting the proposal is what the
/// scenario's own reject path is for, and that one does write back to the ledger.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record ScenarioPreparedTriggerEvent(
    Guid ScenarioId,
    string ScenarioName,
    Guid? ScenarioGroupId,
    string ConditionKind,
    Guid UserId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.ScenarioPrepared;

    public string Severity => AgentTriggerSeverity.Medium;

    public Guid? TargetUserId => UserId;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.ScenarioPrepared;

    /// <summary>
    /// Only the scenario name reaches the sentence. ConditionKind stays in <see cref="Payload"/>: it is
    /// an internal identifier ("uncut_fullday_shift"), and user-facing text carries no internals.
    /// </summary>
    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["scenario"] = ScenarioName
    };

    public string DedupKey => ScenarioId.ToString();

    public string? ActionRoute => ProactiveActionRoutes.Schedule;

    public IReadOnlyDictionary<string, string>? ActionParams
    {
        get
        {
            var actionParams = new Dictionary<string, string>
            {
                [ProactiveActionParamKeys.ScenarioId] = ScenarioId.ToString()
            };

            if (ScenarioGroupId is Guid groupId)
            {
                actionParams[ProactiveActionParamKeys.GroupId] = groupId.ToString();
            }

            return actionParams;
        }
    }

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["scenarioId"] = ScenarioId,
        ["scenarioName"] = ScenarioName,
        ["groupId"] = ScenarioGroupId,
        ["conditionKind"] = ConditionKind
    };
}
