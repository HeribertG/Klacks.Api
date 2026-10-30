// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// A plannable shift of the group that the weekday cut leaves alone, with the reason.
/// </summary>
/// <param name="ShiftId">The plannable shift</param>
/// <param name="Name">Name of the plannable shift</param>
/// <param name="Reason">Why it is not cut</param>

namespace Klacks.Api.Application.Services.Schedules;

public sealed record ShiftWeekdayCutSkip(Guid ShiftId, string Name, string Reason);
