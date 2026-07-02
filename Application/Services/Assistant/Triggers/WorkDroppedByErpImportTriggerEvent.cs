// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A future, not-yet-locked Work entry was cancelled because its order was superseded by an
/// ERP import update. Client (the roster employee) has no login account to notify directly --
/// AppUser and Client are unrelated identities in this system -- so this reaches planners
/// instead, same audience as UnstaffedShiftTriggerEvent, so they can re-plan the gap.
/// </summary>
using System.Globalization;
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record WorkDroppedByErpImportTriggerEvent(
    Guid WorkId,
    string EmployeeName,
    DateOnly Workday) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.WorkDroppedByErpImport;
    public string Severity => AgentTriggerSeverity.High;
    public bool PlannersOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.WorkDroppedByErpImport;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["employee"] = EmployeeName,
        ["date"] = Workday.ToString(ProactiveMessageFormats.DisplayDate, CultureInfo.InvariantCulture)
    };

    public string DedupKey => $"{WorkId}:erp-supersede";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["workId"] = WorkId,
        ["employee"] = EmployeeName,
        ["workday"] = Workday
    };
}
