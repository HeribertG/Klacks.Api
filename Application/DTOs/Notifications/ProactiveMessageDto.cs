// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public record ProactiveMessageDto
{
    public string MessageId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? ConversationId { get; init; }
    public DateTime Timestamp { get; init; }
    public string MessageType { get; init; } = string.Empty;

    /// <summary>
    /// Interpolation values for an i18n <see cref="Content"/> (a content string starting with
    /// <c>i18n:</c>). Null for plain-text messages.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ContentParams { get; init; }

    /// <summary>
    /// Trigger kind that produced this message (see AgentTriggerKinds). Empty for messages that
    /// did not originate from the proactive trigger pipeline.
    /// </summary>
    public string Kind { get; init; } = string.Empty;

    /// <summary>
    /// Frontend route for the one-click action on this message; null when no action is offered.
    /// </summary>
    public string? ActionRoute { get; init; }

    /// <summary>
    /// Parameters accompanying <see cref="ActionRoute"/> (e.g. groupId, clientId, date);
    /// null when the action carries no parameters or no action is offered.
    /// </summary>
    public IReadOnlyDictionary<string, string>? ActionParams { get; init; }
}
