// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Loads one expense of a given scope: the main plan (AnalyseToken null) or exactly one scenario. Unlike
/// GetQuery&lt;ExpensesResource&gt; (REST GET, main plan only) it reaches scenario rows, so the assistant can edit
/// the expenses of the scenario it works in. An expense of another scope, of a hidden owner or a missing one is
/// answered alike with KeyNotFoundException.
/// </summary>
/// <param name="Id">Id of the expense</param>
/// <param name="AnalyseToken">Scope to read in; null = main plan</param>
public record GetExpenseInScopeQuery(Guid Id, Guid? AnalyseToken) : IRequest<ExpensesResource>;
