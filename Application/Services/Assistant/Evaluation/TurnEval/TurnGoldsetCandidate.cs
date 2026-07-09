// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Raw telemetry candidate for the turn goldset: a real user message correlated with the
/// first successful skill execution of the same turn, before human curation.
/// </summary>
/// <param name="Message">Raw user message of the turn</param>
/// <param name="SkillName">Skill the production model executed for this turn</param>
/// <param name="ParametersJson">Serialized tool arguments of the executed skill</param>
/// <param name="ConversationId">String conversation id linking message and skill record</param>
/// <param name="Timestamp">Execution time of the skill record</param>

namespace Klacks.Api.Application.Services.Assistant.Evaluation.TurnEval;

public sealed record TurnGoldsetCandidate(
    string Message,
    string SkillName,
    string? ParametersJson,
    string ConversationId,
    DateTime Timestamp);
