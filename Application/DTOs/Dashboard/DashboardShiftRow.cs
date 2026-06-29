// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Dashboard;

public record DashboardShiftRow(
    DateOnly FromDate,
    DateOnly? UntilDate,
    bool IsMonday,
    bool IsTuesday,
    bool IsWednesday,
    bool IsThursday,
    bool IsFriday,
    bool IsSaturday,
    bool IsSunday);
