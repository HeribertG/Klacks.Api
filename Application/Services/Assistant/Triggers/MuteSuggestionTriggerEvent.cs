// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Asks one specific user whether Klacksy should mute a trigger kind the user dismissed several
/// times in a row. Targeted at a single user (TargetUserId) and dispatched through the existing
/// proactive pipeline; the per-kind dedup key ensures the question is asked at most once per
/// user and kind. The user keeps the decision — nothing is muted automatically.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record MuteSuggestionTriggerEvent(string SuggestedKind, Guid UserId) : IAgentTriggerEvent
{
    private const string DedupKeyPrefix = "mute-suggestion:";

    public string Kind => AgentTriggerKinds.MuteSuggestion;

    public string Severity => AgentTriggerSeverity.Low;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + ProactiveMessageI18nKeys.MuteSuggestion;

    public Guid? TargetUserId => UserId;

    public IReadOnlyDictionary<string, string>? SummaryParams => new Dictionary<string, string>
    {
        ["kind"] = SuggestedKind
    };

    public string DedupKey => DedupKeyPrefix + SuggestedKind;

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["kind"] = SuggestedKind
    };
}
