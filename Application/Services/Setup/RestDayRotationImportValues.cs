// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired RestDayRotationRule row for the region-setup entity import (K10/K20).
/// </summary>
public sealed record RestDayRotationImportValues(
    DayOfWeek DayOfWeek,
    int MinFree,
    int WindowWeeks);
