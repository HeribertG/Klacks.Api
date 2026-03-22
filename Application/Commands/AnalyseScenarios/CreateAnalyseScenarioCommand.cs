// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to create a new AnalyseScenario.
/// </summary>
/// <param name="Request">Contains name, description, group and time period of the scenario</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record CreateAnalyseScenarioCommand(CreateAnalyseScenarioRequest Request) : IRequest<AnalyseScenarioResource>;
