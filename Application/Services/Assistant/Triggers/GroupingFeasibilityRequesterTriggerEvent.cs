// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Grouping feasibility report for the user who asked for it in chat. Addressed to that user only, so it
/// is not ledger-tracked; same kind and sentence as the daily admin report. Fingerprint and counts cover
/// only the part of the report the requester's group scope shows (the whole report for an unrestricted
/// requester), so the requester sees one inbox entry per distinct visible report.
/// </summary>
/// <param name="ReportFingerprint">SHA-256 over the report findings the requester may see; used as dedup key.</param>
/// <param name="Counts">Numbers shown in the inbox sentence, limited to what the requester may see.</param>
/// <param name="RequesterId">User who ran the analysis.</param>

using Klacks.Api.Application.DTOs.Grouping;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record GroupingFeasibilityRequesterTriggerEvent(string ReportFingerprint, GroupingFeasibilityCounts Counts, Guid RequesterId)
    : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.GroupingFeasibility;

    public string Severity => GroupingFeasibilityTriggerParams.SeverityFor(Counts);

    public Guid? TargetUserId => RequesterId;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.GroupingFeasibility;

    public IReadOnlyDictionary<string, string> SummaryParams => GroupingFeasibilityTriggerParams.Summary(Counts);

    public string DedupKey => ReportFingerprint;

    public string? ActionRoute => ProactiveActionRoutes.GroupList;

    public IReadOnlyDictionary<string, string>? ActionParams => null;

    public IReadOnlyDictionary<string, object?> Payload => GroupingFeasibilityTriggerParams.Payload(Counts);
}
