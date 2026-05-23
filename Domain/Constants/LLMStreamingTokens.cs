// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Sentinel tokens for the internal streaming protocol between LLM providers and the
/// LLMService tool-call parser. Providers emit these prefixes around serialized tool-call
/// payloads; LLMService detects them in the token stream to separate tool calls from
/// regular content. Centralized here so the NUL-byte contract cannot diverge between the
/// emitting providers and the consuming parser. The NUL prefix is built via (char)0 so the
/// source file contains neither a raw NUL byte nor an escape sequence that editors could
/// silently corrupt (which is exactly how the Cohere provider once diverged to a space).
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class LLMStreamingTokens
{
    private const char Sentinel = (char)0;

    public static readonly string ToolCallPrefix = Sentinel + "TOOL:";

    public static readonly string ToolCallEnd = Sentinel + "TOOL_END";
}
