// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// A light, optional "by the way" small-talk question that Klacksy asks one specific user to learn
/// a personal interest. Targeted at a single user (TargetUserId) and dispatched through the existing
/// proactive trigger pipeline (per-user preference + rate-limit + notification hub).
/// </summary>

using Klacks.Api.Domain.Constants;
using Klacks.Api.Domain.Interfaces.Assistant;

namespace Klacks.Api.Application.Services.Assistant.Triggers;

public sealed record CuriosityQuestionTriggerEvent(string Question, Guid UserId) : IAgentTriggerEvent
{
    public string Kind => AgentTriggerKinds.CuriosityQuestion;

    public string Severity => AgentTriggerSeverity.Low;

    public string Summary => Question;

    public Guid? TargetUserId => UserId;

    public IReadOnlyDictionary<string, object?> Payload => new Dictionary<string, object?>
    {
        ["question"] = Question,
    };
}
