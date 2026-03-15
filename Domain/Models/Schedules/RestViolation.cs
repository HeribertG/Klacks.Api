// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Ruhezeit-Verletzung zwischen zwei aufeinanderfolgenden Arbeitsblöcken.
/// </summary>
/// <param name="PreviousBlock">Der vorangehende Dienst</param>
/// <param name="NextBlock">Der nachfolgende Dienst</param>
/// <param name="ActualRest">Tatsächliche Ruhezeit zwischen den Blöcken</param>
/// <param name="RequiredRest">Geforderte Mindest-Ruhezeit</param>
namespace Klacks.Api.Domain.Models.Schedules;

public record RestViolation(
    ScheduleBlock PreviousBlock,
    ScheduleBlock NextBlock,
    TimeSpan ActualRest,
    TimeSpan RequiredRest);
