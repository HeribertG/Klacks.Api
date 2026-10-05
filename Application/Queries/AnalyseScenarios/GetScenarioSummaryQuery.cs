// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Query for the live summary of one AnalyseScenario.
/// </summary>
/// <param name="Id">ID of the scenario</param>

using Klacks.Api.Application.DTOs.Schedules.Summary;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.AnalyseScenarios;

public record GetScenarioSummaryQuery(Guid Id) : IRequest<ScenarioSummaryDto?>;
