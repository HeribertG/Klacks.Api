// Copyright (c) Heribert Gasparoli Private. All rights reserved.

/// <summary>
/// Resolves the configured company address (APP_ADDRESS_* settings) to a geographic coordinate,
/// used as a fallback for features that need a location when the user's device location is unavailable.
/// Returns null when no usable company address is configured or the coordinate cannot be resolved.
/// </summary>

namespace Klacks.Api.Domain.Interfaces.Settings;

public interface ICompanyLocationProvider
{
    Task<(double Latitude, double Longitude)?> GetCompanyLocationAsync(CancellationToken cancellationToken = default);
}
