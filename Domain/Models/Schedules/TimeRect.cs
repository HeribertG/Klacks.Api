namespace Klacks.Api.Domain.Models.Schedules;

public record TimeRect(
    Guid SourceId,
    TimeRectSourceType SourceType,
    Guid ClientId,
    DateOnly Date,
    TimeOnly Start,
    TimeOnly End);
