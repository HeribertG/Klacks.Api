// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Events;

public sealed record PeriodClosedEvent(
    DateOnly StartDate,
    DateOnly EndDate,
    Guid? GroupId,
    int WorkCount,
    int BreakCount,
    int SealedDayCount,
    string SealedBy) : DomainEvent;
