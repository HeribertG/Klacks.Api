// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;
using Klacks.Api.Domain.Services.Assistant.Providers;

namespace Klacks.Api.Domain.Models.Assistant;

public class LLMResponse
{
    public string Message { get; set; } = string.Empty;

    public bool ActionPerformed { get; set; }

    public string? ConversationId { get; set; }

    public object? Data { get; set; }

    public List<string>? Suggestions { get; set; }

    public SuggestedRepliesConfig? SuggestedReplies { get; set; }

    public string? NavigateTo { get; set; }

    public string? NavigateToTarget { get; set; }

    public string? MissedTargetId { get; set; }

    public List<object>? FunctionCalls { get; set; }

    /// <summary>
    /// The calls of the turn as the loop executed them, in execution order. FunctionCalls is the wire projection and
    /// drops the flags a post-turn step needs (RequiresConfirmation, SkippedByStop, the rejection flags, the issued
    /// token); this list keeps them for the server and is never serialized.
    /// </summary>
    [JsonIgnore]
    public IReadOnlyList<LLMFunctionCall>? ExecutedCalls { get; set; }

    public LLMUsageInfo? Usage { get; set; }
}
