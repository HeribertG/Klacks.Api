// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <param name="PeriodFrom">Start of the date range to load (inclusive).</param>
/// <param name="PeriodUntil">End of the date range to load (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main schedule.</param>
/// <param name="Language">UI language; null falls back to English.</param>
/// <param name="ContextDaysBefore">
/// Days before <paramref name="PeriodFrom"/> loaded as boundary context for future boundary-aware validators.
/// The bitmap stays sized to the period; boundary works/breaks land in BitmapInput.BoundaryAssignments. Default 14.
/// </param>
/// <param name="ContextDaysAfter">Same as <paramref name="ContextDaysBefore"/>, after <paramref name="PeriodUntil"/>.</param>
public sealed record HolisticHarmonizerRunInput(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string? Language,
    int ContextDaysBefore = 14,
    int ContextDaysAfter = 14);
