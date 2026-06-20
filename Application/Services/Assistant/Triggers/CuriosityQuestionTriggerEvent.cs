// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A light, optional "by the way" small-talk question that Klacksy asks one specific user to learn
/// a personal interest. Targeted at a single user (TargetUserId) and dispatched through the existing
/// proactive trigger pipeline. The Summary is an i18n key (not literal text) so the frontend renders
/// the question in the user's UI language.
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record CuriosityQuestionTriggerEvent(string Topic, Guid UserId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.CuriosityQuestion;

    public string Severity => AgentTriggerSeverity.Low;

    public string Summary => ProactiveMessageMarkers.I18nPrefix + CuriosityQuestions.KeyFor(Topic);

    public Guid? TargetUserId => UserId;

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["topic"] = Topic,
    };
}
