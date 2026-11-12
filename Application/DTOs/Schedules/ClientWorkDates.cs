// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// One client and the calendar days it works on, for a check of several clients in one call.
/// </summary>
/// <param name="ClientId">Employee to judge</param>
/// <param name="WorkDates">Calendar days the client has work on</param>
public sealed record ClientWorkDates(Guid ClientId, IReadOnlyCollection<DateOnly> WorkDates);
