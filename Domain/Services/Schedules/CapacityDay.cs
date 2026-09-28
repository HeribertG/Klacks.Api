// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

namespace Klacks.Api.Domain.Services.Schedules;

public readonly record struct CapacityDay(
    DateOnly Date,
    double DesiredReadiness,
    double Demand,
    double ExistingAbsence);
