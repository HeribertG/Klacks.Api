// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Builds an in-memory lookup that decides whether a (client, date) entry is period-closed,
/// based on SealedDay rows with Level=Closed that apply either globally or via group membership.
/// @param fromDate - Lower bound (inclusive) of the date range to evaluate
/// @param untilDate - Upper bound (inclusive) of the date range to evaluate
/// @param clientIds - Clients whose group memberships are resolved for group-scoped seals
/// </summary>
namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPeriodClosedEntryFilter
{
    Task<IPeriodClosedLookup> BuildAsync(
        DateOnly fromDate,
        DateOnly untilDate,
        IReadOnlyCollection<Guid> clientIds,
        CancellationToken cancellationToken = default);
}
