// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Command to rename an existing AnalyseScenario.
/// </summary>
/// <param name="Id">ID of the scenario to rename</param>
/// <param name="Name">New name for the scenario</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record RenameAnalyseScenarioCommand(Guid Id, string Name) : IRequest<AnalyseScenarioResource>;
