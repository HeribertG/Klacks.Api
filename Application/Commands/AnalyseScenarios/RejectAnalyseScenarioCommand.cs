// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to reject an AnalyseScenario.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to reject</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record RejectAnalyseScenarioCommand(Guid ScenarioId) : IRequest<bool>;
