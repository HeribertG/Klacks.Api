// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

/// <param name="ModelId">Internal model id used as picker value.</param>
/// <param name="DisplayName">Human-readable label.</param>
/// <param name="ProviderId">Owning provider id (Anthropic, Groq, Google, ...).</param>
/// <param name="IsHealthy">True if the model responded to the tiny text ping in time.</param>
/// <param name="LatencyMs">Round-trip latency in milliseconds; 0 when the call could not start.</param>
/// <param name="ContextWindow">Maximum context window in tokens.</param>
/// <param name="CostPerInputToken">Provider cost per 1000 input tokens; 0 means the model is free.</param>
/// <param name="CostPerOutputToken">Provider cost per 1000 output tokens; 0 means the model is free.</param>
/// <param name="Error">Failure reason when <see cref="IsHealthy"/> is false; null on success.</param>
public sealed record SpeechModelCheckDto(
    string ModelId,
    string DisplayName,
    string ProviderId,
    bool IsHealthy,
    long LatencyMs,
    int ContextWindow,
    decimal CostPerInputToken,
    decimal CostPerOutputToken,
    string? Error);
