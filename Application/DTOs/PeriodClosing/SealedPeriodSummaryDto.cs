// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.PeriodClosing;

/// <summary>
/// Daily summary of sealing state for the period overview calendar in the frontend.
/// </summary>
public class SealedPeriodSummaryDto
{
    public DateOnly Date { get; set; }

    public int TotalWorkCount { get; set; }

    public int SealedWorkCount { get; set; }

    public int TotalBreakCount { get; set; }

    public int SealedBreakCount { get; set; }

    public bool IsFullySealed => TotalWorkCount > 0 && TotalWorkCount == SealedWorkCount;
}
