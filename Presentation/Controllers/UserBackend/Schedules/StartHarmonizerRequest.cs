// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <param name="PeriodFrom">Start date (inclusive)</param>
/// <param name="PeriodUntil">End date (inclusive)</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null = main scenario</param>

namespace Klacks.Api.Presentation.Controllers.UserBackend.Schedules;

public sealed record StartHarmonizerRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken);
