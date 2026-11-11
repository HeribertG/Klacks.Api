// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.Constants;
using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Request payload for a Holistic Harmonizer MVP run. Mirrors the Wizard 2 context request and adds the
/// LLM model id and the UI language so the LLM produces reason texts the operator can read.
/// </summary>
/// <param name="PeriodFrom">Start of the date range to load (inclusive).</param>
/// <param name="PeriodUntil">End of the date range to load (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main schedule.</param>
/// <param name="LlmModelId">Model id to send the prompt to (resolved from app settings by the caller).</param>
/// <param name="Language">UI language, used so swap reasons come back in the operator's locale.</param>
/// <param name="ContextDaysBefore">Boundary context days before <paramref name="PeriodFrom"/> for boundary-aware validators. Default 14.</param>
/// <param name="ContextDaysAfter">Boundary context days after <paramref name="PeriodUntil"/>. Default 14.</param>
/// <param name="KeywordMode">Hard (default) vetoes the restricting schedule commands; Soft treats them as wishes.</param>
/// <param name="GroupId">Group the run harmonises for; null = no group (see HarmonizerContextRequest.GroupId).</param>
/// <param name="AgentOrderIsUserDefined">True keeps the hand-arranged AgentIds order as the row order (see
/// HarmonizerContextRequest.AgentOrderIsUserDefined); false ranks automatically.</param>
public sealed record HolisticHarmonizerEngineRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string LlmModelId,
    string? Language,
    int ContextDaysBefore = ScenarioConstants.BoundaryDays,
    int ContextDaysAfter = ScenarioConstants.BoundaryDays,
    KeywordMode KeywordMode = KeywordMode.Hard,
    Guid? GroupId = null,
    bool AgentOrderIsUserDefined = false);
