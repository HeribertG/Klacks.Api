// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Grouping;

/// <summary>
/// Number of shifts placed into one city group.
/// </summary>
/// <param name="GroupName">Name of the city group.</param>
/// <param name="GroupId">Id of the city group.</param>
/// <param name="ShiftCount">Number of shifts placed into it.</param>
public sealed record ShiftCityGroupTargetSummary(string GroupName, Guid GroupId, int ShiftCount);
