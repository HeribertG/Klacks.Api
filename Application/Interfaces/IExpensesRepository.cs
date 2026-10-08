// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IExpensesRepository : IBaseRepository<Expenses>
{
    /// <summary>
    /// Loads an expense (untracked) with its parent Work regardless of its scope: unlike Get, scenario rows
    /// (AnalyseToken set) are returned too. For paths that must reach scenario expenses; untracked, so a later
    /// full-row Put of the same id in the same scope cannot hit a tracking conflict.
    /// </summary>
    /// <param name="id">Id of the expense</param>
    Task<Expenses?> GetWithWorkInAnyScope(Guid id);

    /// <summary>
    /// Lists the expenses of exactly one scope (untracked): the main plan when analyseToken is null, otherwise that
    /// scenario. The scope is the parent Work's AnalyseToken, not the expense's own column, so legacy rows whose own
    /// token drifted from their Work still land in the right scope. Never mixes main-plan and scenario rows.
    /// </summary>
    /// <param name="analyseToken">Scope to list; null = main plan</param>
    /// <param name="cancellationToken">Cancellation token</param>
    Task<List<Expenses>> ListInScopeAsync(Guid? analyseToken, CancellationToken cancellationToken = default);
}
