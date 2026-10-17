// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Assistant;

/// <summary>
/// Usage facts about one user from which the explanation-depth heuristic estimates experience.
/// </summary>
/// <param name="OwnWorkCount">Live real-plan works (no scenario) the user created</param>
/// <param name="FirstWorkCreatedUtc">Creation instant of the user's oldest such work; null when there is none</param>
/// <param name="LastWorkCreatedUtc">Creation instant of the user's newest such work; null when there is none</param>
/// <param name="SuccessfulSkillWrites">Successful Klacksy skill executions of category Crud or Action by the user</param>
public sealed record UserExperienceSignals(
    int OwnWorkCount,
    DateTime? FirstWorkCreatedUtc,
    DateTime? LastWorkCreatedUtc,
    int SuccessfulSkillWrites)
{
    public static readonly UserExperienceSignals None = new(0, null, null, 0);

    public int ActiveSpanDays => FirstWorkCreatedUtc.HasValue && LastWorkCreatedUtc.HasValue
        ? (int)(LastWorkCreatedUtc.Value - FirstWorkCreatedUtc.Value).TotalDays
        : 0;
}
