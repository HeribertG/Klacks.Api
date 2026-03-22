// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query for retrieving a single AnalyseScenario by ID.
/// </summary>
/// <param name="Id">ID of the requested scenario</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.AnalyseScenarios;

public record GetAnalyseScenarioQuery(Guid Id) : IRequest<AnalyseScenarioResource?>;
