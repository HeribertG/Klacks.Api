// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Lists the expenses of exactly one scope: the main plan (AnalyseToken null) or one scenario. Only expenses whose
/// parent Work is owned by a client inside the caller's group visibility are returned. ListQuery&lt;ExpensesResource&gt;
/// (REST GET /Expenses) is the main-plan case of this query.
/// </summary>
/// <param name="AnalyseToken">Scope to list; null = main plan</param>
public record ListExpensesInScopeQuery(Guid? AnalyseToken) : IRequest<IEnumerable<ExpensesResource>>;
