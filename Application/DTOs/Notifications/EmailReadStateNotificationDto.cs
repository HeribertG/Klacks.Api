// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Notifications;

public class EmailReadStateNotificationDto
{
    public Guid EmailId { get; set; }
    public bool IsRead { get; set; }
    public string Folder { get; set; } = string.Empty;
    public DateTime Timestamp { get; set; }
}
