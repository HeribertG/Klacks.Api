// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Enums;
using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record RouteSegmentDirections(
    string FromName,
    string ToName,
    string TransportMode,
    double DistanceKm,
    TimeSpan Duration,
    List<DirectionStep> Steps);
