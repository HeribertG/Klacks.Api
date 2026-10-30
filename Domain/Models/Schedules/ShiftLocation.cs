// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// Where a task shift takes place: the coordinates of its customer's first address.
/// </summary>
/// <param name="ShiftId">The task shift</param>
/// <param name="Latitude">Latitude in degrees</param>
/// <param name="Longitude">Longitude in degrees</param>
public sealed record ShiftLocation(Guid ShiftId, double Latitude, double Longitude);
