// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Notifications;

public class NewEmailsNotificationDto
{
    public int Count { get; set; }
    public DateTime Timestamp { get; set; }
}
