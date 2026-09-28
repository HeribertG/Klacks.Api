// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
