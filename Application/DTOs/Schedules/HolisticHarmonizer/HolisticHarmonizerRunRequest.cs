// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules.HolisticHarmonizer;

/// <param name="PeriodFrom">Start date (inclusive).</param>
/// <param name="PeriodUntil">End date (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null = main scenario.</param>
/// <param name="Language">UI language; the LLM writes its swap reasons in this locale.</param>
public sealed record HolisticHarmonizerRunRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string? Language);
