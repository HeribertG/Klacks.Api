// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Geo;

public sealed record AddressCluster(
    string Country,
    string State,
    string City,
    double? Latitude,
    double? Longitude,
    int DirectCount,
    int AttachedCount);
