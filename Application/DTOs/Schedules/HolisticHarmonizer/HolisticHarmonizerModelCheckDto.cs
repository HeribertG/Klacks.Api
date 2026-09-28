// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

public sealed record HolisticHarmonizerModelCheckDto(
    string ModelId,
    string DisplayName,
    string ProviderId,
    bool IsHealthy,
    long LatencyMs,
    string? Error,
    decimal? EvalCompositeScore = null);
