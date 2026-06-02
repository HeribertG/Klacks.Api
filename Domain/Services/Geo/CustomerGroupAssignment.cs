// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Geo;

public record CustomerGroupAssignment(Guid ClientId, Guid GroupId, double DistanceKm);
