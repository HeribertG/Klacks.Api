// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.Schedules;

/// <summary>
/// Loads one WorkChange of a given scope: the main plan (AnalyseToken null) or exactly one scenario. Unlike
/// GetQuery&lt;WorkChangeResource&gt; (REST GET, main plan only) it reaches scenario rows, so the assistant can
/// edit the changes of the scenario it works in. A change of another scope, of a hidden owner or a missing one is
/// answered alike with KeyNotFoundException.
/// </summary>
/// <param name="Id">Id of the WorkChange</param>
/// <param name="AnalyseToken">Scope to read in; null = main plan</param>
public record GetWorkChangeInScopeQuery(Guid Id, Guid? AnalyseToken) : IRequest<WorkChangeResource>;
