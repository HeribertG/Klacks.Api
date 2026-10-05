// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record ResolvedHolidayCalendarSource(
    HolidayCalendarSource Source,
    Guid? CalendarSelectionId,
    string? CalendarName,
    string? Country,
    string? State);
