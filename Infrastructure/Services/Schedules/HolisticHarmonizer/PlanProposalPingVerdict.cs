// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Infrastructure.Services.Schedules.HolisticHarmonizer;

/// <summary>
/// Outcome of evaluating one pre-flight ping response.
/// </summary>
/// <param name="IsHealthy">True when the model answered the ping or proved reachable while thinking</param>
/// <param name="Error">Why the ping failed; null when healthy</param>
/// <param name="OutputBudgetSpentOnThinking">True when the output-token limit cut the answer after the model reasoned</param>
public sealed record PlanProposalPingVerdict(bool IsHealthy, string? Error, bool OutputBudgetSpentOnThinking);
