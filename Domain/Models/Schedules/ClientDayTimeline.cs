namespace Klacks.Api.Domain.Models.Schedules;

public class ClientDayTimeline
{
    private static readonly TimeSpan FullDay = TimeSpan.FromHours(24);

    public Guid ClientId { get; }
    public DateOnly Date { get; }
    public List<TimeRect> Rects { get; } = [];

    public ClientDayTimeline(Guid clientId, DateOnly date)
    {
        ClientId = clientId;
        Date = date;
    }

    public List<(TimeRect A, TimeRect B)> GetCollisions()
    {
        var collisions = new List<(TimeRect A, TimeRect B)>();
        var workRects = Rects
            .Where(r => r.SourceType is TimeRectSourceType.Work or TimeRectSourceType.Correction or TimeRectSourceType.Replacement)
            .ToList();

        for (var i = 0; i < workRects.Count - 1; i++)
        {
            for (var j = i + 1; j < workRects.Count; j++)
            {
                if (workRects[i].SourceId == workRects[j].SourceId) continue;

                if (TimeRectsOverlap(workRects[i], workRects[j]))
                {
                    collisions.Add((workRects[i], workRects[j]));
                }
            }
        }

        return collisions;
    }

    private static bool TimeRectsOverlap(TimeRect a, TimeRect b)
    {
        var aStart = a.Start.ToTimeSpan();
        var aEnd = a.End < a.Start
            ? a.End.ToTimeSpan().Add(FullDay)
            : a.End.ToTimeSpan();
        var bStart = b.Start.ToTimeSpan();
        var bEnd = b.End < b.Start
            ? b.End.ToTimeSpan().Add(FullDay)
            : b.End.ToTimeSpan();

        return aStart < bEnd && bStart < aEnd;
    }
}
