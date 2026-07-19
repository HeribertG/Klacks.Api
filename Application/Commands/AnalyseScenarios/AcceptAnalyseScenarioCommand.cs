// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to accept an AnalyseScenario.
/// </summary>
/// <param name="ScenarioId">ID of the scenario to accept</param>
/// <param name="OverrideBlock">Requests the K1 supervisor override for Block-mode compliance violations</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record AcceptAnalyseScenarioCommand(Guid ScenarioId, bool OverrideBlock = false) : IRequest<bool>;
