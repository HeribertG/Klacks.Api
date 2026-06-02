// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Application.Services.Grouping;

public interface IGroupGeocoder
{
    Task<(double? Latitude, double? Longitude)> GeocodeAsync(string placeName, CancellationToken cancellationToken = default);
}
