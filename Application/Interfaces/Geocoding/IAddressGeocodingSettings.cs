// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

/// <summary>
/// Reads the installation's geocoding privacy switch on every call (no caching), so a changed setting applies to
/// the next scan, request or Klacksy call.
/// </summary>

namespace Klacks.Api.Application.Interfaces.Geocoding;

public interface IAddressGeocodingSettings
{
    Task<bool> AreEmployeeAddressesAllowedAsync(CancellationToken cancellationToken = default);
}
