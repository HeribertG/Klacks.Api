// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Email;

public class ReceivedEmailListResponse
{
    public List<ReceivedEmailListResource> Items { get; set; } = [];

    public int TotalCount { get; set; }

    public int UnreadCount { get; set; }
}
