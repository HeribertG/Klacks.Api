// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Email;

public class ReceivedEmailListResponse
{
    public List<ReceivedEmailListResource> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int UnreadCount { get; set; }
}
