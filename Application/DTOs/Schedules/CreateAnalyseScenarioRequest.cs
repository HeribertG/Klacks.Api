// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Request-DTO zum Erstellen eines neuen AnalyseScenarios.
/// </summary>
/// <param name="Name">Name des Szenarios</param>
/// <param name="GroupId">Gruppe, fuer die das Szenario erstellt wird</param>
/// <param name="FromDate">Startdatum des Szenario-Zeitraums</param>
/// <param name="UntilDate">Enddatum des Szenario-Zeitraums</param>

namespace Klacks.Api.Application.DTOs.Schedules;

public class CreateAnalyseScenarioRequest
{
    public string Name { get; set; } = string.Empty;

    public string? Description { get; set; }

    public Guid GroupId { get; set; }

    public DateOnly FromDate { get; set; }

    public DateOnly UntilDate { get; set; }
}
