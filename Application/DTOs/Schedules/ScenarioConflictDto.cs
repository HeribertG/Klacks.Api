// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A single rule violation found in a scenario, projected for evaluate_scenario.
/// </summary>
/// <param name="ClientId">Affected employee</param>
/// <param name="ClientName">Name of the affected employee</param>
/// <param name="Date">Day of the violation (ISO yyyy-MM-dd)</param>
/// <param name="Severity">Error, Warning or Info</param>
/// <param name="Code">Stable machine code of the violation</param>
/// <param name="MessageKey">Translation key for display</param>
/// <param name="MessageParams">Parameters for the translation key</param>
public sealed record ScenarioConflictDto(
    Guid ClientId,
    string ClientName,
    string Date,
    string Severity,
    string Code,
    string MessageKey,
    IReadOnlyDictionary<string, string> MessageParams);
