// Copyright (c) Heribert Gasparoli Private. All rights reserved.

using Klacks.Api.Domain.Enums;

namespace Klacks.Api.Application.Services.Setup;

/// <summary>
/// Value payload of one desired CounterRule row for the region-setup entity import (K18/K20).
/// </summary>
public sealed record CounterRuleImportValues(
    CounterEventType EventType,
    CounterPeriod Period,
    int Threshold,
    decimal? HoursThreshold);
