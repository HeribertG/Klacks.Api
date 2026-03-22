// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query for retrieving all AnalyseScenarios of a specific group.
/// </summary>
/// <param name="GroupId">ID of the group whose scenarios are retrieved</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.AnalyseScenarios;

public record ListAnalyseScenariosQuery(Guid GroupId) : IRequest<List<AnalyseScenarioResource>>;
