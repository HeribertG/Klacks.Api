// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Wire names of the Server-Sent Events the chat stream emits, and the mapping from SseChunkType to
/// them. Extracted from the controller so the wire contract has one source of truth that tests can
/// assert against.
/// </summary>

using Klacks.Api.Domain.Models.Assistant;

namespace Klacks.Api.Domain.Constants;

public static class SseEventNames
{
    public const string StreamStart = "stream_start";
    public const string Content = "content";
    public const string FunctionCall = "function_call";
    public const string FunctionResult = "function_result";
    public const string Metadata = "metadata";
    public const string Done = "done";
    public const string Error = "error";
    public const string Status = "status";

    /// <summary>
    /// Name an unmapped chunk type is sent under. Kept so a newly added chunk type reaches the client
    /// as an ignorable event instead of throwing mid-stream.
    /// </summary>
    public const string Unknown = "unknown";

    /// <param name="type">Chunk type to translate into its SSE event name</param>
    public static string For(SseChunkType type) => type switch
    {
        SseChunkType.StreamStart => StreamStart,
        SseChunkType.Content => Content,
        SseChunkType.FunctionCall => FunctionCall,
        SseChunkType.FunctionResult => FunctionResult,
        SseChunkType.Metadata => Metadata,
        SseChunkType.Done => Done,
        SseChunkType.Error => Error,
        SseChunkType.Status => Status,
        _ => Unknown
    };
}
