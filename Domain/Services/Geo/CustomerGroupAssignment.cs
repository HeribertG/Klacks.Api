// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

public record CustomerGroupAssignment(Guid ClientId, Guid GroupId, double DistanceKm);
