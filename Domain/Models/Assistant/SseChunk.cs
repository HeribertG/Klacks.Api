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
    public Guid? UiActionTrackingId { get; set; }
    public LLMUsageInfo? Usage { get; set; }
    public List<string>? Suggestions { get; set; }
    public SuggestedRepliesConfig? SuggestedReplies { get; set; }
    public string? NavigateTo { get; set; }
    public string? Target { get; set; }
    public string? MissedTargetId { get; set; }
    public bool ActionPerformed { get; set; }
    public List<object>? FunctionCalls { get; set; }
    public string? ErrorMessage { get; set; }
    public string? Stage { get; set; }
    public long? ElapsedMs { get; set; }
    public int? Iteration { get; set; }

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

    public static SseChunk FunctionResultChunk(string functionName, string? result, string executionType, string? uiActionSteps = null, Guid? uiActionTrackingId = null) => new()
    {
        Type = SseChunkType.FunctionResult,
        FunctionName = functionName,
        FunctionResult = result,
        ExecutionType = executionType,
        UiActionSteps = uiActionSteps,
        UiActionTrackingId = uiActionTrackingId
    };

    public static SseChunk Metadata(LLMResponse response) => new()
    {
        Type = SseChunkType.Metadata,
        Usage = response.Usage,
        Suggestions = response.Suggestions,
        SuggestedReplies = response.SuggestedReplies,
        NavigateTo = response.NavigateTo,
        Target = response.NavigateToTarget,
        MissedTargetId = response.MissedTargetId,
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

    /// <summary>
    /// Progress event emitted while the turn is still working on the answer. Advisory only: it carries
    /// no answer content, and a client that ignores it loses nothing but the progress display.
    /// </summary>
    /// <param name="stage">One of the SseStatusStages keys; never display text, the client localizes it</param>
    /// <param name="elapsedMs">Milliseconds since the turn started, omitted when no turn clock is available</param>
    /// <param name="iteration">1-based tool-loop iteration, set only on stages that repeat per iteration</param>
    public static SseChunk Status(string stage, long? elapsedMs = null, int? iteration = null) => new()
    {
        Type = SseChunkType.Status,
        Stage = stage,
        ElapsedMs = elapsedMs,
        Iteration = iteration
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
    Error,
    Status
}
