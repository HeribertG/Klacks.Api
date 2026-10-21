// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record DistanceMatrix(
    List<Location> Locations,
    double[,] Matrix,
    double[,] DurationMatrix,
    Dictionary<string, double[,]>? DurationMatricesByProfile = null,
    bool IsEstimated = false);
