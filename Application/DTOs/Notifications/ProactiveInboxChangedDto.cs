// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public record ProactiveInboxChangedDto
{
    public int UnreadCount { get; init; }
}
