// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Schedules.Wizard3;

/// <summary>
/// Request payload for a Wizard 3 MVP run. Mirrors the Wizard 2 context request and adds the
/// LLM model id and the UI language so the LLM produces reason texts the operator can read.
/// </summary>
/// <param name="PeriodFrom">Start of the date range to load (inclusive).</param>
/// <param name="PeriodUntil">End of the date range to load (inclusive).</param>
/// <param name="AgentIds">Clients whose rows are part of the bitmap.</param>
/// <param name="AnalyseToken">Source-scenario isolation token; null reads the main schedule.</param>
/// <param name="LlmModelId">Model id to send the prompt to (resolved from app settings by the caller).</param>
/// <param name="Language">UI language, used so swap reasons come back in the operator's locale.</param>
public sealed record Wizard3EngineRequest(
    DateOnly PeriodFrom,
    DateOnly PeriodUntil,
    IReadOnlyList<Guid> AgentIds,
    Guid? AnalyseToken,
    string LlmModelId,
    string? Language);
