// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Assistant.Planning;

/// <summary>
/// Atomic step in an AgentPlan: a single skill-call plus optional verify pairing.
/// Serialized as JSON into AgentPlan.StepsJson.
/// </summary>
/// <param name="Order">1-based execution order.</param>
/// <param name="Skill">Skill name (matches a row in agent_skills).</param>
/// <param name="Params">Parameter map. Values may be literals or placeholders like "$prev.id" / "$prev.scenarioId" referring to the previous step's result.</param>
/// <param name="VerifySkill">Optional read-skill name to call right after to confirm the mutation took effect.</param>
/// <param name="Reversible">Whether the step can be safely rolled back. Non-reversible steps trigger HITL approval.</param>
public sealed record PlanStep(
    int Order,
    string Skill,
    Dictionary<string, object?> Params,
    string? VerifySkill,
    bool Reversible);
