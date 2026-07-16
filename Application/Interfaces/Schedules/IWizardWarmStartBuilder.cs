// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Loads the last accepted real plan of the period preceding <paramref name="periodFrom"/> and maps it,
/// weekday-true, onto the current planning period so it can seed the Wizard 1 warm-start population.
/// The previous plan is always read from the real ledger (AnalyseToken == null), even for scenario runs.
/// </summary>
public interface IWizardWarmStartBuilder
{
    /// <summary>
    /// Builds the warm-start assignments for the given agents and target period.
    /// </summary>
    /// <param name="agentIds">Client ids to include (same axis as the wizard request)</param>
    /// <param name="periodFrom">Target period start (inclusive)</param>
    /// <param name="periodUntil">Target period end (inclusive)</param>
    /// <param name="ct">Cancellation token</param>
    /// <returns>Assignments already mapped onto the target period; empty when there is no prior plan.</returns>
    Task<IReadOnlyList<CoreWarmStartAssignment>> BuildAsync(
        IReadOnlyList<Guid> agentIds,
        DateOnly periodFrom,
        DateOnly periodUntil,
        CancellationToken ct);
}
