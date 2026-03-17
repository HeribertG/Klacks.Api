// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command zum Ablehnen eines AnalyseScenarios.
/// </summary>
/// <param name="ScenarioId">ID des abzulehnenden Szenarios</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record RejectAnalyseScenarioCommand(Guid ScenarioId) : IRequest<bool>;
