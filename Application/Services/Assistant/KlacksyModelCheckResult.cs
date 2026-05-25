// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant;

/// <param name="ModelId">The Klacks-internal model id (picker value).</param>
/// <param name="DisplayName">Human-readable label.</param>
/// <param name="ProviderId">Owning provider.</param>
/// <param name="IsHealthy">True when the model answered the probe request without an error.</param>
/// <param name="SupportsToolCalling">True when the model emitted the expected function call for the probe.</param>
/// <param name="LatencyMs">Round-trip time in milliseconds; 0 when the call could not start.</param>
/// <param name="ContextWindow">Maximum context window in tokens.</param>
/// <param name="CostPerInputToken">Cost per 1000 input tokens (0 = free).</param>
/// <param name="CostPerOutputToken">Cost per 1000 output tokens (0 = free).</param>
/// <param name="Qualifies">True when the model meets every Klacksy readiness criterion.</param>
/// <param name="Error">Failure reason; null on success.</param>
public sealed record KlacksyModelCheckResult(
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
    string? Error);
