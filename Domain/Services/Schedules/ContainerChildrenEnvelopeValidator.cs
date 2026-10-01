// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Enforces the container rule "a container covers all of its items without overhang": every sub-work and
/// sub-break must lie completely inside the container envelope. Times are compared relative to the envelope
/// start, so envelopes crossing midnight work; an envelope with equal start and end covers the whole day.
/// </summary>
/// <param name="envelopeStart">Start of the container work</param>
/// <param name="envelopeEnd">End of the container work</param>
/// <param name="items">Start and end of each sub-work and sub-break</param>

namespace Klacks.Api.Domain.Services.Schedules;

public static class ContainerChildrenEnvelopeValidator
{
    private const int MinutesPerHour = 60;
    private const int MinutesPerDay = 24 * MinutesPerHour;

    public static IReadOnlyList<(TimeOnly Start, TimeOnly End)> FindOverhangs(
        TimeOnly envelopeStart,
        TimeOnly envelopeEnd,
        IEnumerable<(TimeOnly Start, TimeOnly End)> items)
    {
        var origin = ToMinutes(envelopeStart);
        var envelopeLength = SpanMinutes(origin, ToMinutes(envelopeEnd));
        if (envelopeLength == 0)
        {
            return Array.Empty<(TimeOnly, TimeOnly)>();
        }

        return items
            .Where(item =>
            {
                var itemStart = ToMinutes(item.Start);
                var offset = SpanMinutes(origin, itemStart);
                var length = SpanMinutes(itemStart, ToMinutes(item.End));
                return offset + length > envelopeLength;
            })
            .ToList();
    }

    private static int ToMinutes(TimeOnly time) => time.Hour * MinutesPerHour + time.Minute;

    private static int SpanMinutes(int from, int to) => (to - from + MinutesPerDay) % MinutesPerDay;
}
