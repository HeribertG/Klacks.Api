// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// An ERP import file (or one order within it) could not be processed. A data/integration
/// concern, not a scheduling gap -- reaches Admins only, not the wider planner audience.
/// </summary>
using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record OrderImportFailedTriggerEvent(
    Guid ExceptionId,
    string FileKey,
    string Reason) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.OrderImportFailed;
    public string Severity => AgentTriggerSeverity.High;
    public bool AdminOnly => true;
    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.OrderImportFailed;

    public IReadOnlyDictionary<string, string> SummaryParams => new Dictionary<string, string>
    {
        ["file"] = FileKey,
        ["reason"] = Reason
    };

    public string DedupKey => $"{ExceptionId}:import-failed";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["exceptionId"] = ExceptionId,
        ["file"] = FileKey,
        ["reason"] = Reason
    };
}
