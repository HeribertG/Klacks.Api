// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Proactive "you just did X — shall I also do Y?" suggestion event, emitted from a learned active
/// sequential skill edge. Dispatched through the existing proactive trigger pipeline (per-user
/// preference + rate-limit + notification hub), so no dedicated UI is required.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record SkillSequenceSuggestionTriggerEvent(string FromLabel, string ToLabel) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.SkillSequenceSuggestion;

    public string Severity => AgentTriggerSeverity.Low;

    public string Summary => $"You just did \"{FromLabel}\" — shall I also do \"{ToLabel}\"?";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["fromLabel"] = FromLabel,
        ["toLabel"] = ToLabel,
    };
}
