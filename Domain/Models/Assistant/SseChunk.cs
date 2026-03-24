// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Represents a single Server-Sent Event chunk for LLM chat streaming.
/// @param Type - The event type determining how the frontend processes this chunk
/// @param Text - Token delta text for Content events
/// </summary>

namespace Klacks.Api.Domain.Models.Assistant;

public class SseChunk
{
    public SseChunkType Type { get; set; }
    public string? Text { get; set; }
    public string? ConversationId { get; set; }
    public string? FunctionName { get; set; }
    public Dictionary<string, object>? Parameters { get; set; }
    public string? FunctionResult { get; set; }
    public string? ExecutionType { get; set; }
    public string? UiActionSteps { get; set; }
    public LLMUsageInfo? Usage { get; set; }
    public List<string>? Suggestions { get; set; }
    public SuggestedRepliesConfig? SuggestedReplies { get; set; }
    public string? NavigateTo { get; set; }
    public bool ActionPerformed { get; set; }
    public List<object>? FunctionCalls { get; set; }
    public string? ErrorMessage { get; set; }

    public static SseChunk StreamStart(string conversationId) => new()
    {
        Type = SseChunkType.StreamStart,
        ConversationId = conversationId
    };

    public static SseChunk Content(string text) => new()
    {
        Type = SseChunkType.Content,
        Text = text
    };

    public static SseChunk FunctionCallChunk(string functionName, Dictionary<string, object>? parameters) => new()
    {
        Type = SseChunkType.FunctionCall,
        FunctionName = functionName,
        Parameters = parameters
    };

    public static SseChunk FunctionResultChunk(string functionName, string? result, string executionType, string? uiActionSteps = null) => new()
    {
        Type = SseChunkType.FunctionResult,
        FunctionName = functionName,
        FunctionResult = result,
        ExecutionType = executionType,
        UiActionSteps = uiActionSteps
    };

    public static SseChunk Metadata(LLMResponse response) => new()
    {
        Type = SseChunkType.Metadata,
        Usage = response.Usage,
        Suggestions = response.Suggestions,
        SuggestedReplies = response.SuggestedReplies,
        NavigateTo = response.NavigateTo,
        ActionPerformed = response.ActionPerformed,
        FunctionCalls = response.FunctionCalls
    };

    public static SseChunk Done() => new()
    {
        Type = SseChunkType.Done
    };

    public static SseChunk Error(string message) => new()
    {
        Type = SseChunkType.Error,
        ErrorMessage = message
    };
}

public enum SseChunkType
{
    StreamStart,
    Content,
    FunctionCall,
    FunctionResult,
    Metadata,
    Done,
    Error
}
