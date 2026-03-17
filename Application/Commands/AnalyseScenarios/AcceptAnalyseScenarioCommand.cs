// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command zum Akzeptieren eines AnalyseScenarios.
/// </summary>
/// <param name="ScenarioId">ID des zu akzeptierenden Szenarios</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record AcceptAnalyseScenarioCommand(Guid ScenarioId) : IRequest<bool>;
