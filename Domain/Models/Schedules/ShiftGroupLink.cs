// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Models.Schedules;

/// <summary>
/// One live link of a shift to a group, read as a projection with the validity the link carries.
/// </summary>
/// <param name="ShiftId">The linked shift</param>
/// <param name="GroupId">The group</param>
/// <param name="ValidFrom">First day of the link; null = no lower bound</param>
/// <param name="ValidUntil">Last day of the link; null = open end</param>
public sealed record ShiftGroupLink(Guid ShiftId, Guid GroupId, DateTime? ValidFrom, DateTime? ValidUntil);
