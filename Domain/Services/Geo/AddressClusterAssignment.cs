// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

public sealed record AddressClusterAssignment(
    Guid ClientId,
    string Country,
    string State,
    string City,
    bool AttachedByDistance,
    double? DistanceKm);
