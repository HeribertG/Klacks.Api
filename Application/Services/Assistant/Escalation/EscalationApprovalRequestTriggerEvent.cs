// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The event EscalationNotifier writes to the inbox and live-pushes for one stage of a ProactiveApproval
/// chain. Same Kind as the absence stage alert - it IS an escalation stage - but its own i18n key, because
/// the sentence asks a different question: not "can you cover this shift?" but "may Klacksy run this
/// remediation?". Never dispatched through AgentTriggerService.OnEventAsync and never composed for a
/// messenger: an approval request reaches the inbox and, for a connected recipient, the live push only.
/// The frontend renders the key in the recipient's UI language from SummaryParams.
/// </summary>
/// <param name="StageId">The stage being notified; keys the dedup entry.</param>
/// <param name="UserId">The roster candidate asked to approve.</param>
/// <param name="ConditionId">The ledger row the approval is about.</param>
/// <param name="TriggerKind">The finding's kind, shown as the "finding" parameter.</param>
/// <param name="RemediationSkillName">What Klacksy would do, shown as the "action" parameter.</param>
/// <param name="DueAtUtc">When this stage's turn ends and the next candidate is asked.</param>
/// <param name="CompanyTimeZone">Zone the due time is rendered in.</param>

using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Settings;

namespace Klacks.Api.Application.Services.Assistant.Escalation;

public sealed record EscalationApprovalRequestTriggerEvent(
    Guid StageId,
    string UserId,
    Guid ConditionId,
    string TriggerKind,
    string RemediationSkillName,
    DateTime DueAtUtc,
    TimeZoneInfo CompanyTimeZone) : IAgentTriggerEvent
{
    public const string FindingParameter = EscalationHandoffPlaceholders.Finding;
    public const string ActionParameter = EscalationHandoffPlaceholders.Action;
    public const string DueTimeParameter = "dueTime";

    private const string DueTimeFormat = "HH:mm";
    private const string DedupSuffix = ":escalation-approval-request";

    public string Kind => AgentTriggerKinds.EscalationStageAlert;
    public string Severity => AgentTriggerSeverity.High;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.EscalationApprovalRequest;

    public Guid? TargetUserId => Guid.TryParse(UserId, out var id) ? id : null;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        [FindingParameter] = TriggerKind,
        [ActionParameter] = RemediationSkillName,
        [DueTimeParameter] = TimeZoneInfo.ConvertTimeFromUtc(DueAtUtc, CompanyTimeZone)
            .ToString(DueTimeFormat, CultureInfo.InvariantCulture) + " " + IanaTimeZoneId.From(CompanyTimeZone)
    };

    public string DedupKey => StageId + DedupSuffix;

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["stageId"] = StageId,
        ["conditionId"] = ConditionId,
        ["triggerKind"] = TriggerKind,
        ["remediationSkillName"] = RemediationSkillName,
        ["dueAtUtc"] = DueAtUtc
    };
}
