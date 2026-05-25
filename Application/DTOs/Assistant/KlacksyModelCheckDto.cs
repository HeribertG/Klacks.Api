// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Assistant;

/// <param name="ModelId">Internal model id used as picker value.</param>
/// <param name="DisplayName">Human-readable label.</param>
/// <param name="ProviderId">Owning provider id (Anthropic, Google, OpenAI, ...).</param>
/// <param name="IsHealthy">True if the model answered the Klacksy readiness probe without an error.</param>
/// <param name="SupportsToolCalling">True if the model emitted the expected function call for the probe.</param>
/// <param name="LatencyMs">Round-trip latency in milliseconds; 0 when the call could not start.</param>
/// <param name="ContextWindow">Maximum context window in tokens.</param>
/// <param name="CostPerInputToken">Provider cost per 1000 input tokens; 0 means the model is free.</param>
/// <param name="CostPerOutputToken">Provider cost per 1000 output tokens; 0 means the model is free.</param>
/// <param name="Qualifies">True when the model is strong enough to drive Klacksy.</param>
/// <param name="IsEnabled">Resulting enabled state after the optimization was applied.</param>
/// <param name="IsDefault">True when this model is the resulting default model.</param>
/// <param name="Error">Failure reason when <see cref="IsHealthy"/> is false; null on success.</param>
public sealed record KlacksyModelCheckDto(
    string ModelId,
    string DisplayName,
    string ProviderId,
    bool IsHealthy,
    bool SupportsToolCalling,
    long LatencyMs,
    int ContextWindow,
    decimal CostPerInputToken,
    decimal CostPerOutputToken,
    bool Qualifies,
    bool IsEnabled,
    bool IsDefault,
    string? Error);
