// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Zeitraum mit Unterbesetzung — weniger aktive Mitarbeiter als gefordert.
/// </summary>
/// <param name="Start">Beginn der Unterbesetzung</param>
/// <param name="End">Ende der Unterbesetzung</param>
/// <param name="StaffCount">Tatsächliche Besetzung im Zeitraum</param>
/// <param name="RequiredStaff">Geforderte Mindestbesetzung</param>
namespace Klacks.Api.Domain.Models.Schedules;

public record UnderstaffedPeriod(
    DateTime Start,
    DateTime End,
    int StaffCount,
    int RequiredStaff);
