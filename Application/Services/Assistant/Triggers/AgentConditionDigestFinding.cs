// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// One condition-ledger row named individually in a planner's daily digest (the "top 3-5" short list),
/// carried in AgentConditionDigestTriggerEvent.Payload as structured data rather than spelled out in the
/// rendered sentence: TriggerKind is a raw kind slug that would need its own translation to read as
/// prose, and IAgentTriggerEvent.SummaryParams only substitutes scalar values into one i18n template.
/// </summary>

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record AgentConditionDigestFinding(
    string TriggerKind,
    Guid? EntityId,
    Guid? GroupId,
    string Severity,
    DateTime DetectedAtUtc);
