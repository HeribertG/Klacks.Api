// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Assistant.Planning;

/// <summary>
/// Lightweight view of one valid step inside AgentPlan.StepsJson, carrying only what proposal rendering and
/// persistence decisions need.
/// </summary>
/// <param name="Skill">Non-blank skill name of the step.</param>
/// <param name="VerifySkill">Optional verify skill name, null when absent or not a string.</param>
public sealed record PlanStepSummary(string Skill, string? VerifySkill);
