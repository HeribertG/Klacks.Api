// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <param name="ModelId">The Klacks-internal model id (used as picker value).</param>
/// <param name="DisplayName">Human-readable label for the table.</param>
/// <param name="ProviderId">Owning provider — useful for grouping/filtering in the UI.</param>
/// <param name="IsHealthy">True only when the model returned a parseable ping JSON within the timeout.</param>
/// <param name="LatencyMs">Round-trip time in milliseconds; 0 when the call could not start.</param>
/// <param name="Error">Failure reason when <see cref="IsHealthy"/> is false; null on success.</param>
public sealed record HolisticHarmonizerModelCheckResult(
    string ModelId,
    string DisplayName,
    string ProviderId,
    bool IsHealthy,
    long LatencyMs,
    string? Error);
