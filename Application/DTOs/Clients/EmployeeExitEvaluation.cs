// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Clients;

/// <summary>
/// Outcome of advising on one employee exit. The facts (memberships, bookings after the exit, contracts, groups) are kept
/// apart from the findings and the recommendation, which are computed deterministically; nothing is changed.
/// </summary>
/// <param name="ClientId">Identifier of the employee, to pass on to the recording step</param>
/// <param name="Facts">What was found</param>
/// <param name="Findings">Deterministic findings, most important first</param>
/// <param name="Recommendation">Conservative summary of what the exit means</param>
public sealed record EmployeeExitEvaluation(
    Guid ClientId,
    EmployeeExitFacts Facts,
    IReadOnlyList<EmployeeExitFinding> Findings,
    string Recommendation);
