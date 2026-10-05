// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.DTOs.Schedules;

public sealed record HolidayOutcomeWork(DateOnly WorkDate, TimeOnly StartTime, TimeOnly EndTime, bool StartsTheDayBefore);
