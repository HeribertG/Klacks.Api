// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Domain.Interfaces.RouteOptimization;

public record GeocodingSearchResult(
    GeocodingSearchOutcome Status,
    double? Latitude,
    double? Longitude,
    string? DisplayName);
