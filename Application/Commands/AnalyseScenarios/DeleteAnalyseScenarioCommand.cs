// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command zum Loeschen eines AnalyseScenarios.
/// </summary>
/// <param name="ScenarioId">ID des zu loeschenden Szenarios</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record DeleteAnalyseScenarioCommand(Guid ScenarioId) : IRequest<bool>;
