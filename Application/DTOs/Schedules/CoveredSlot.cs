// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// A slot of the absent employee that cover_absence proposed a replacement for (a Replacement
/// WorkChange in the scenario).
/// </summary>
/// <param name="ShiftId">The shift being covered (original shift id)</param>
/// <param name="Date">Workday</param>
/// <param name="ReplacementClientId">Employee proposed to take over</param>
/// <param name="ReplacementName">Display name of the replacement</param>
/// <param name="Tier">Escalation tier this cover needed, as an int so the DTO stays engine-free:
/// 0 in-group free, 1 in-group swap, 2 cross-group free, 3 cross-group swap. Lets the UI show how far
/// the engine had to reach instead of presenting every cover as equally cheap.</param>
public sealed record CoveredSlot(
    Guid ShiftId,
    DateOnly Date,
    Guid ReplacementClientId,
    string ReplacementName,
    int Tier = 0);
