// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// The single JsonSerializerOptions instance every SSE chunk of the chat stream is written with.
/// Shared instead of built per request so the wire shape (camelCase names, null fields omitted) has
/// one source of truth that tests assert against, and so the options cache is reused.
/// </summary>

using System.Text.Json;
using System.Text.Json.Serialization;

namespace Klacks.Api.Domain.Models.Assistant;

public static class SseChunkJson
{
    // Not sealed with MakeReadOnly(): that overload demands an explicit TypeInfoResolver and throws in
    // the static constructor without one (caught by SseStatusChunkTests). System.Text.Json freezes the
    // instance itself on the first serialization, which is the same protection one call later.
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };
}
