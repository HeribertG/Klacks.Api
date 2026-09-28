// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Assistant;

public class GetConversationHistoryRequest
{
    public int Limit { get; set; } = 10;

    public int Offset { get; set; } = 0;

    public string? UserId { get; set; }
}