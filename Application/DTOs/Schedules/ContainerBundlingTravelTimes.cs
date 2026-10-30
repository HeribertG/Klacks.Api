// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Whether the advice rests on travel times of the routing service. NotCalculated covers an unreachable or switched off
/// service, where only straight-line estimates exist, and a plan that never got as far as the distance matrix.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContainerBundlingTravelTimes>))]
public enum ContainerBundlingTravelTimes
{
    NotCalculated,
    Calculated,
}
