// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public record ProactiveMessageDto
{
    public string MessageId { get; init; } = string.Empty;
    public string Content { get; init; } = string.Empty;
    public string? ConversationId { get; init; }
    public DateTime Timestamp { get; init; }
    public string MessageType { get; init; } = string.Empty;
}
