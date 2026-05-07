// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

public sealed record HolisticHarmonizerModelCheckDto(
    string ModelId,
    string DisplayName,
    string ProviderId,
    bool IsHealthy,
    long LatencyMs,
    string? Error);
