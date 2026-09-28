// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

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
