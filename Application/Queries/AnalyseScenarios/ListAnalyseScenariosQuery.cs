// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Query for retrieving AnalyseScenarios, optionally filtered by group.
/// </summary>
/// <param name="GroupId">Optional group filter. Null returns all scenarios including group-unfiltered ones.</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.AnalyseScenarios;

public record ListAnalyseScenariosQuery(Guid? GroupId) : IRequest<List<AnalyseScenarioResource>>;
