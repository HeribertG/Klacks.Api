// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Address geocoding queue of an instance whose AddressGeocoding background service is switched off:
/// nothing would drain a real queue. It accepts nothing and reports every address as not queued.
/// </summary>

using Klacks.Api.Application.Interfaces;

namespace Klacks.Api.Infrastructure.Services;

public class DisabledAddressGeocodingQueue : IAddressGeocodingQueue
{
    public bool TryQueue(Guid addressId) => false;
}
