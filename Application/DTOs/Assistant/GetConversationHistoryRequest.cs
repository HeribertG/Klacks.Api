// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Constants;

namespace Klacks.Api.Application.DTOs.Assistant;

public class GetConversationHistoryRequest
{
    public int Limit { get; set; } = AssistantLimitDefaults.ConversationHistoryRequestLimit;

    public int Offset { get; set; } = 0;

    public string? UserId { get; set; }
}