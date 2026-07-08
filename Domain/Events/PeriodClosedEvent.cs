// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Events;

public sealed record PeriodClosedEvent(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? GroupId,
    int WorkCount,
    int BreakCount,
    int SealedDayCount,
    string SealedBy) : DomainEvent;
