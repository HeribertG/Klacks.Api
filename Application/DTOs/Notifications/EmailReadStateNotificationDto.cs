// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public class EmailReadStateNotificationDto
{
    public Guid EmailId { get; set; }
    public bool IsRead { get; set; }
    public string Folder { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
