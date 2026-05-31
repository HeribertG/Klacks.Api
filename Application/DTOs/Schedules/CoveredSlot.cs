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
public sealed record CoveredSlot(Guid ShiftId, DateOnly Date, Guid ReplacementClientId, string ReplacementName);
