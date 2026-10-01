// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Queue of client addresses to geocode in the background (coordinates, and the state when empty).
/// TryQueue never waits: it returns false when the queue is full or no worker drains it on this
/// instance, so a caller can report truthfully how many addresses were queued.
/// </summary>

namespace Klacks.Api.Application.Interfaces;

public interface IAddressGeocodingQueue
{
    bool TryQueue(Guid addressId);
}
