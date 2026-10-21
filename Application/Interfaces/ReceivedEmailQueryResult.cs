// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Email;

namespace Klacks.Api.Application.Interfaces;

public class ReceivedEmailQueryResult
{
    public List<ReceivedEmail> Items { get; set; } = [];
    public int TotalCount { get; set; }
    public int UnreadCount { get; set; }
}
