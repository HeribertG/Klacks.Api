// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Immutable lookup over pre-computed sets of globally closed dates and (client, date)
/// pairs closed via group-scoped SealedDay rows.
/// @param globallyClosedDates - Dates sealed for all clients (SealedDay.GroupId = null)
/// @param closedClientDates - (client, date) pairs sealed via group membership
/// </summary>
using Klacks.Api.Application.Interfaces.Exports;

namespace Klacks.Api.Infrastructure.Services.Exports;

public class PeriodClosedLookup : IPeriodClosedLookup
{
    private readonly HashSet<DateOnly> _globallyClosedDates;
    private readonly HashSet<(Guid ClientId, DateOnly Date)> _closedClientDates;

    public PeriodClosedLookup(
        HashSet<DateOnly> globallyClosedDates,
        HashSet<(Guid ClientId, DateOnly Date)> closedClientDates)
    {
        _globallyClosedDates = globallyClosedDates;
        _closedClientDates = closedClientDates;
    }

    public bool IsClosed(Guid clientId, DateOnly date)
    {
        return _globallyClosedDates.Contains(date) || _closedClientDates.Contains((clientId, date));
    }
}
