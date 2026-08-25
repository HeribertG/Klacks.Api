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
///
/// PlannersOnly is set alongside the target, exactly as AgentConditionDigestTriggerEvent does and for
/// the same reason: it changes nothing about the audience (TargetUserId short-circuits
/// ResolveRecipientsAsync before PlannersOnly is read) and nothing about the ledger exclusion
/// (IsLedgerTracked checks TargetUserId first), but it is what AgentTriggerService.IsCompanionEvent
/// reads. Without it this Medium-severity note would be classified as companion chatter, which
/// IsLoudEvent admits regardless of severity - every connected planner would get an interrupting chat
/// push for "I have prepared something", louder than an unstaffed shift three days out. It belongs in
/// the inbox with its one-click action, not in front of somebody mid-task.
/// </summary>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record ScenarioPreparedTriggerEvent(
    Guid ScenarioId,
    DateOnly FromDate,
    Guid? ScenarioGroupId,
    string ConditionKind,
    Guid UserId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.ScenarioPrepared;

    public string Severity => AgentTriggerSeverity.Medium;

    public Guid? TargetUserId => UserId;

    public bool PlannersOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.ScenarioPrepared;

    /// <summary>
    /// The sentence names the day the proposal covers, never the scenario's name and never
    /// ConditionKind. Both can carry an internal identifier - the auto-generated name spells the kind
    /// into itself - and user-facing text carries no internals. The date is always meaningful, whoever
    /// named the scenario; the identifiers stay in <see cref="Payload"/>.
    /// </summary>
    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["from"] = FromDate.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture)
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
        ["fromDate"] = FromDate,
        ["groupId"] = ScenarioGroupId,
        ["conditionKind"] = ConditionKind
    };
}
