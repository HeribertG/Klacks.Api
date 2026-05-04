// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to rename an existing AnalyseScenario.
/// </summary>
/// <param name="Id">ID of the scenario to rename</param>
/// <param name="Name">New name for the scenario</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record RenameAnalyseScenarioCommand(Guid Id, string Name) : IRequest<AnalyseScenarioResource>;
