// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired RestDayRotationRule row for the region-setup entity import (K10/K20).
/// </summary>
public sealed record RestDayRotationImportValues(
    DayOfWeek DayOfWeek,
    int MinFree,
    int WindowWeeks);
