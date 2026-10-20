// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// How many employees of a group have a sealed day (day approval or closed period) on one date.
/// </summary>
/// <param name="Date">The sealed date</param>
/// <param name="PersonCount">Number of employees of the group for whom the date is sealed</param>
public sealed record PlanningSealedDayCount(DateOnly Date, int PersonCount);
