// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Proactive "you just did X — shall I also do Y?" suggestion event, emitted from a learned active
/// sequential skill edge. Targeted at the user who executed the skill (TargetUserId), so the
/// suggestion never leaks to other connected users. Dispatched through the existing proactive
/// trigger pipeline (per-user preference + rate-limit + notification hub), so no dedicated UI is
/// required.
/// The labels are AgentSkill descriptions, which are written for the LLM and run to a thousand
/// characters, so each one is capped before it enters the sentence. Capping the labels rather than
/// the finished sentence is what keeps the suggestion itself intact: a cap applied afterwards would
/// cut the message off inside the first label and drop the "shall I also do Y" half entirely.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;
using Klacks.Api.Domain.Services.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record SkillSequenceSuggestionTriggerEvent(string FromLabel, string ToLabel, Guid UserId) : IAgentTriggerEvent
{
    /// <summary>
    /// Per-label cap. Both labels plus the surrounding sentence have to stay inside
    /// ProactiveTriggerDispatchLimits.ContentKeyMaxLength, which this leaves ample room for.
    /// </summary>
    private const int LabelMaxLength = 200;

    public string Kind => AgentTriggerKinds.SkillSequenceSuggestion;

    public string Severity => AgentTriggerSeverity.Low;

    public Guid? TargetUserId => UserId;

    public string Summary => $"You just did \"{Cap(FromLabel)}\" — shall I also do \"{Cap(ToLabel)}\"?";

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["fromLabel"] = FromLabel,
        ["toLabel"] = ToLabel,
    };

    private static string Cap(string label)
        => ProactiveTextTruncator.Cap(label, LabelMaxLength) ?? string.Empty;
}
