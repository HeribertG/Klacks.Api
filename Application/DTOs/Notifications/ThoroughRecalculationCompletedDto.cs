// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.DTOs.Notifications;

public record ThoroughRecalculationCompletedDto
{
    public DateOnly StartDate { get; init; }
    public DateOnly EndDate { get; init; }
    public Guid? SelectedGroup { get; init; }
    public Guid? AnalyseToken { get; init; }
    public int ProcessedWorks { get; init; }
    public int ProcessedWorkChanges { get; init; }
    public int ProcessedBreaks { get; init; }
}
