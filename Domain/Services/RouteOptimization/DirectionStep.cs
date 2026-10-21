// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using Klacks.Api.Domain.Models.Schedules;

namespace Klacks.Api.Domain.Services.RouteOptimization;

public record DirectionStep(
    string Instruction,
    string StreetName,
    double DistanceMeters,
    int DurationSeconds,
    string ManeuverType);
