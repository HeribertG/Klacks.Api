// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Geo;

public sealed record AddressClusterAssignment(
    Guid ClientId,
    string Country,
    string State,
    string City,
    bool AttachedByDistance,
    double? DistanceKm);
