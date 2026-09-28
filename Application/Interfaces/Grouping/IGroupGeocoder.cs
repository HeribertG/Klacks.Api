// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Application.Interfaces.Grouping;

public interface IGroupGeocoder
{
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(string placeName, CancellationToken cancellationToken = default);
}
