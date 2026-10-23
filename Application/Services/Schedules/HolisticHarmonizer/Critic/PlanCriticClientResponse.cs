// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Schedules.HolisticHarmonizer.Critic;

/// <summary>
/// Raw, still unverified answer of the vision critic.
/// </summary>
/// <param name="Findings">Findings as parsed from the model answer; not yet checked against the plan.</param>
/// <param name="Error">Technical reason when the call or the parse failed; null on success.</param>
public sealed record PlanCriticClientResponse(IReadOnlyList<PlanCriticFinding> Findings, string? Error)
{
    public static PlanCriticClientResponse Failure(string error) => new([], error);
}
