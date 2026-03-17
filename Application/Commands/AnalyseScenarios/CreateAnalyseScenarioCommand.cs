// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Command zum Erstellen eines neuen AnalyseScenarios.
/// </summary>
/// <param name="Request">Enthaelt Name, Beschreibung, Gruppe und Zeitraum des Szenarios</param>

using Klacks.Api.Application.DTOs.Schedules;
using Klacks.Api.Infrastructure.Mediator;

namespace Klacks.Api.Application.Commands.AnalyseScenarios;

public record CreateAnalyseScenarioCommand(CreateAnalyseScenarioRequest Request) : IRequest<AnalyseScenarioResource>;
