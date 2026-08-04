// Copyright (c) Heribert Gasparoli Private. All rights reserved.

namespace Klacks.Api.Domain.Services.Schedules;

public readonly record struct CapacityDay(
    DateOnly Date,
    double DesiredReadiness,
    double Demand,
    double ExistingAbsence);
