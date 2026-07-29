// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Bounds and defaults for the company-wide monthly target hours table.
/// </summary>

namespace Klacks.Api.Domain.Constants;

public static class MonthlyTargetHoursConstants
{
    public const int MinimumMonth = 1;

    public const int MaximumMonth = 12;

    public const int MinimumYear = 1900;

    public const int MaximumYear = 2999;

    public const decimal MinimumHours = 0m;

    public const decimal FullWorkloadPercent = 100m;
}
