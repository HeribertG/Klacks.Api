// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command to delete all AnalyseScenarios of a group.
/// </summary>
/// <param name="GroupId">Optional group ID. If null, all scenarios are deleted.</param>

using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record DeleteAllAnalyseScenariosCommand(Guid? GroupId) : IRequest<bool>;
