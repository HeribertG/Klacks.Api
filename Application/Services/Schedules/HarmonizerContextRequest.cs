// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules;

/// <summary>
/// Request payload that drives a harmonizer (Wizard 2) run. The harmonizer reads the
/// already-saved schedule for the listed agents and date range, builds an internal bitmap,
/// and improves harmony via a genetic algorithm. AnalyseToken isolates the source schedule
/// when running on a non-main scenario.
/// </summary>
/// <param name="PeriodFrom">Start of the date range the harmonizer reads (inclusive)</param>
/// <param name="PeriodUntil">End of the date range the harmonizer reads (inclusive)</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main scenario</param>
public sealed record HarmonizerContextRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken);
