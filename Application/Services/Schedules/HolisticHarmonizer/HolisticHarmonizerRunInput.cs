// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <param name="PeriodFrom">Start of the date range to load (inclusive).</param>
/// <param name="PeriodUntil">End of the date range to load (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main schedule.</param>
/// <param name="Language">UI language; null falls back to English.</param>
public sealed record HolisticHarmonizerRunInput(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string? Language);
