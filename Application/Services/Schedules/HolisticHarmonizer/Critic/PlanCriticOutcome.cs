// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Result of one vision-critic review. Only verified findings are carried; findings whose cells do not match the
/// real plan are dropped and counted in <see cref="DiscardedFindingCount"/>.
/// </summary>
/// <param name="Status">Whether the critic ran, answered or failed.</param>
/// <param name="ModelId">Model that reviewed the plan; null when the critic was skipped before a model was chosen.</param>
/// <param name="Findings">Verified findings only.</param>
/// <param name="DiscardedFindingCount">Findings dropped because a cited cell was out of range or showed another symbol.</param>
/// <param name="Message">Reason for a skipped or failed review; null on success.</param>
public sealed record PlanCriticOutcome(
    PlanCriticStatus Status,
    string? ModelId,
    IReadOnlyList<PlanCriticFinding> Findings,
    int DiscardedFindingCount,
    string? Message)
{
    public static PlanCriticOutcome Skipped(string reason, string? modelId = null)
        => new(PlanCriticStatus.Skipped, modelId, [], 0, reason);

    public static PlanCriticOutcome Failed(string? modelId, string reason)
        => new(PlanCriticStatus.Failed, modelId, [], 0, reason);

    public static PlanCriticOutcome Completed(string modelId, IReadOnlyList<PlanCriticFinding> findings, int discarded)
        => new(PlanCriticStatus.Completed, modelId, findings, discarded, null);
}
