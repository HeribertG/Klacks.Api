// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Pure formula behind the planning deadline: send date = period start - announcement - transit, planning
/// done by = send date - review buffer. No I/O, no clock; the caller supplies today so the result is
/// deterministic and independent of any time zone.
/// </summary>
/// <param name="periodStart">First day of the period the plan is for</param>
/// <param name="today">The company's own local day</param>
/// <param name="announcementDays">Days before the period start the employees must hold the plan</param>
/// <param name="transitDays">Delivery time including printing; 0 for e-mail</param>
/// <param name="reviewDays">Internal check and approval buffer between finished plan and dispatch</param>
/// <param name="nextStartAfter">Maps the last day of a period to the start of the following one</param>
/// <param name="firstPeriodStart">Start of the first period to try</param>
/// <param name="periodEnd">Maps a period start to that period's last day</param>
/// <param name="complianceMinLeadDays">Legal minimum publication lead, 0 when not configured</param>

namespace Klacks.Api.Domain.Services.Assistant;

public static class PlanningDeadlineCalculator
{
    public const int MaxDays = 365;
    public const int InputCount = 3;
    public const int MaxLeadDays = MaxDays * InputCount;
    public const int MaxPeriodAdvance = 400;

    public static PlanningDeadline Compute(
        DateOnly periodStart, DateOnly today, int announcementDays, int transitDays, int reviewDays)
    {
        var sendBy = periodStart.AddDays(-(announcementDays + transitDays));
        var doneBy = sendBy.AddDays(-reviewDays);

        return new PlanningDeadline(periodStart, sendBy, doneBy, doneBy.DayNumber - today.DayNumber);
    }

    public static int LeadDays(int announcementDays, int transitDays, int reviewDays)
    {
        return announcementDays + transitDays + reviewDays;
    }

    public static bool IsValidDays(int days)
    {
        return days >= 0 && days <= MaxDays;
    }

    public static int EffectiveAnnouncement(int announcementDays, int complianceMinLeadDays)
    {
        return Math.Max(announcementDays, complianceMinLeadDays);
    }

    public static PlanningDeadline FirstReachable(
        Func<DateOnly, DateOnly> nextStartAfter,
        DateOnly firstPeriodStart,
        DateOnly today,
        int announcementDays,
        int transitDays,
        int reviewDays,
        Func<DateOnly, DateOnly> periodEnd)
    {
        var start = firstPeriodStart;
        var deadline = Compute(start, today, announcementDays, transitDays, reviewDays);
        for (var advanced = 0; deadline.DaysRemaining < 0 && advanced < MaxPeriodAdvance; advanced++)
        {
            start = nextStartAfter(periodEnd(start));
            deadline = Compute(start, today, announcementDays, transitDays, reviewDays);
        }

        return deadline;
    }
}
