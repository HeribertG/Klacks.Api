// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// The world a scenario clone copies from. Without a token it is the real plan. With a token it is a source scenario
/// that owns the dates of its own range: inside [From, Until] the clone copies the source scenario's rows, outside it
/// the real plan's rows (a scenario holds only stale boundary copies there), so the child is the real plan with the
/// source scenario's changes on top and accepting it never drops real rows the source never carried.
/// </summary>
/// <param name="Token">Source scenario token; null = real plan</param>
/// <param name="From">First day the source scenario owns (ignored without a token)</param>
/// <param name="Until">Last day the source scenario owns (ignored without a token)</param>

namespace Klacks.Api.Infrastructure.Services.AnalyseScenarios;

internal sealed record ScenarioCloneSource(Guid? Token, DateOnly From, DateOnly Until)
{
    public static ScenarioCloneSource RealPlan { get; } = new(null, DateOnly.MinValue, DateOnly.MaxValue);
}
