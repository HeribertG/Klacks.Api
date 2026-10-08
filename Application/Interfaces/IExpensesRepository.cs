// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Interfaces;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Application.Interfaces;

public interface IExpensesRepository : IBaseRepository<Expenses>
{
    /// <summary>
    /// Loads an expense with its parent Work regardless of its scope: unlike Get, scenario rows
    /// (AnalyseToken set) are returned too. For write paths that must reach scenario expenses.
    /// </summary>
    /// <param name="id">Id of the expense</param>
    Task<Expenses?> GetWithWorkInAnyScope(Guid id);
}
