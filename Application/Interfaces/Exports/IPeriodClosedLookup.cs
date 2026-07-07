// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// In-memory lookup answering whether a (client, date) pair falls into a closed period.
/// @param clientId - Identifier of the client the entry belongs to
/// @param date - Calendar date of the entry
/// </summary>
namespace Klacks.Api.Application.Interfaces.Exports;

public interface IPeriodClosedLookup
{
    bool IsClosed(Guid clientId, DateOnly date);
}
