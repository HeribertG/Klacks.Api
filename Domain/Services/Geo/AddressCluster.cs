// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Geo;

public sealed record AddressCluster(
    string Country,
    string State,
    string City,
    double? Latitude,
    double? Longitude,
    int DirectCount,
    int AttachedCount);
