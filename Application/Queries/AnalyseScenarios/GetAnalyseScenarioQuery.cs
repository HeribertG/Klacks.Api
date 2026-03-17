// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query zum Abrufen eines einzelnen AnalyseScenarios per ID.
/// </summary>
/// <param name="Id">ID des gesuchten Szenarios</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.AnalyseScenarios;

public record GetAnalyseScenarioQuery(Guid Id) : IRequest<AnalyseScenarioResource?>;
