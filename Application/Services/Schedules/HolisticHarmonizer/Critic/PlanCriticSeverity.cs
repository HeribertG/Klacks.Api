// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// How strongly the vision critic rates a finding. Purely informational: no severity changes the plan.
/// </summary>
public enum PlanCriticSeverity
{
    Low = 0,
    Medium = 1,
    High = 2,
}
