// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Calculates paid container work duration: envelope span minus contained unpaid breaks.
/// Handles midnight-wrap for both envelope and individual breaks.
/// </summary>

namespace Klacks.Api.Domain.Services.Schedules;

public static class ContainerWorkTimeCalculator
{
    private const int MinutesPerDay = 1440;
    private const int MinutesPerHour = 60;

    public static decimal CalculatePaidHours(
        TimeOnly envelopeStart,
        TimeOnly envelopeEnd,
        IEnumerable<(TimeOnly Start, TimeOnly End)> unpaidBreaks)
    {
        int envelopeMinutes = SpanMinutes(envelopeStart, envelopeEnd);
        if (envelopeMinutes == 0)
        {
            envelopeMinutes = MinutesPerDay;
        }

        int startOffset = ToMinutes(envelopeStart);
        int totalUnpaid = 0;

        foreach (var (bStart, bEnd) in unpaidBreaks)
        {
            int breakMinutes = SpanMinutes(bStart, bEnd);
            if (breakMinutes == 0)
            {
                continue;
            }

            int offset = ((ToMinutes(bStart) - startOffset) + MinutesPerDay) % MinutesPerDay;
            if (offset >= envelopeMinutes)
            {
                continue;
            }
            if (offset + breakMinutes > envelopeMinutes)
            {
                continue;
            }

            totalUnpaid += breakMinutes;
        }

        int paidMinutes = Math.Max(0, envelopeMinutes - totalUnpaid);
        return paidMinutes / (decimal)MinutesPerHour;
    }

    private static int ToMinutes(TimeOnly t) => t.Hour * MinutesPerHour + t.Minute;

    private static int SpanMinutes(TimeOnly start, TimeOnly end)
    {
        int s = ToMinutes(start);
        int e = ToMinutes(end);
        if (s == e)
        {
            return 0;
        }
        return e > s ? e - s : (MinutesPerDay - s) + e;
    }
}
