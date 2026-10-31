// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Single place that turns a recorded acceptance into the scenario's plan: the accepting candidate becomes the
/// replacement of that one slot. Writes through the mediator and the unit of work, so the caller runs it inside the
/// transaction of the recorded answer. Nobody is contacted.
/// </summary>
public interface IScenarioReplacementSwitcher
{
    /// <summary>
    /// Makes the row's candidate the replacement of the row's slot when the row is an acceptance in an active scenario
    /// whose slot still carries a replacement.
    /// </summary>
    /// <param name="row">The saved request row</param>
    /// <param name="overrideBlock">K1 supervisor override for a Block-mode compliance exclusion of the candidate</param>
    /// <param name="cancellationToken">Cancellation token</param>
    /// <returns>True when the candidate is the slot's replacement afterwards</returns>
    Task<bool> ApplyAcceptedAsync(ReplacementRequest row, bool overrideBlock, CancellationToken cancellationToken);
}
