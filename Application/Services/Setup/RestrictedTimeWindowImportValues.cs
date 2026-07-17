// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired RestrictedTimeWindowRule row for the region-setup entity import (K16/K20).
/// </summary>
public sealed record RestrictedTimeWindowImportValues(
    int SeasonFromMonth,
    int SeasonFromDay,
    int SeasonToMonth,
    int SeasonToDay,
    TimeOnly DailyStart,
    TimeOnly DailyEnd,
    string AppliesToGroupTag);
