// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Query zum Abrufen aller AnalyseScenarios einer bestimmten Gruppe.
/// </summary>
/// <param name="GroupId">ID der Gruppe, deren Szenarien abgerufen werden</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Queries.AnalyseScenarios;

public record ListAnalyseScenariosQuery(Guid GroupId) : IRequest<List<AnalyseScenarioResource>>;
