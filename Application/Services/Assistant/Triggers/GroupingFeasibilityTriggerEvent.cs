// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Daily grouping feasibility report for all administrators. Ledger-tracked (AdminOnly without a
/// target user), deduplicated by the report fingerprint: the same set of report findings is announced
/// once, a changed set resolves the old condition and opens a new one. The action route opens the group
/// list; the chat button of this kind sends the report prompt (Klacks.Ui, PROACTIVE_TRIGGER_KIND).
/// </summary>
/// <param name="ReportFingerprint">SHA-256 over the report-only findings.</param>
/// <param name="Counts">Numbers shown in the inbox sentence.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record GroupingFeasibilityTriggerEvent(string ReportFingerprint, GroupingFeasibilityCounts Counts) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.GroupingFeasibility;

    public string Severity => GroupingFeasibilityTriggerParams.SeverityFor(Counts);

    public bool AdminOnly => true;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.GroupingFeasibility;

    public IReadOnlyDictionary<string, string> SummaryParams => GroupingFeasibilityTriggerParams.Summary(Counts);

    public string DedupKey => ReportFingerprint;

    public Guid? GroupId => null;

    public string? ActionRoute => ProactiveActionRoutes.GroupList;

    public IReadOnlyDictionary<string, string>? ActionParams => null;

    public IReadOnlyDictionary<string, object?> Payload => GroupingFeasibilityTriggerParams.Payload(Counts);
}
