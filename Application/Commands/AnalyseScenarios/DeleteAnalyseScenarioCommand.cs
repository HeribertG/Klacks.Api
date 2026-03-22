// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to delete an AnalyseScenario.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to delete</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record DeleteAnalyseScenarioCommand(Guid ScenarioId) : IRequest<bool>;
