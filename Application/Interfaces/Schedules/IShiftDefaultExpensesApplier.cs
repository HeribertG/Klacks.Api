// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces.Schedules;

/// <summary>
/// Copies the default expenses of a shift (ShiftExpenses) onto newly created Works.
/// </summary>
public interface IShiftDefaultExpensesApplier
{
    /// <summary>
    /// Stages one Expenses row per default expense of each Work's shift; it does not save. Only top-level Works
    /// (ParentWorkId null) receive expenses - container children never do. Each expense inherits the Work's
    /// AnalyseToken so scenario Works keep their expenses inside the scenario. The templates of all shifts are
    /// loaded in one query.
    /// </summary>
    /// <param name="works">Newly added Works, already tracked by the caller's unit of work</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task ApplyAsync(IReadOnlyCollection<Work> works, CancellationToken cancellationToken = default);
}
