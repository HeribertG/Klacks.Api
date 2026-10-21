// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.ScheduleOptimizer.Models;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Builds a <see cref="CoreWizardContext"/> from the current DB state for a given wizard run.
/// All DB I/O happens here so the optimizer receives pure, in-memory data.
/// </summary>
public interface IWizardContextBuilder
{
    /// <summary>
    /// Assembles the full wizard context: agents, shifts, contract days (per date), schedule commands,
    /// shift preferences, break blockers and locked existing works.
    /// </summary>
    /// <param name="request">Period, group/agent scope, optional AnalyseToken for scenario isolation</param>
    /// <param name="ct">Cancellation token</param>
    Task<CoreWizardContext> BuildContextAsync(WizardContextRequest request, CancellationToken ct);
}
