// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// Why a service stays out of the tour. New members are appended at the end.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContainerBundlingLeftOutReason>))]
public enum ContainerBundlingLeftOutReason
{
    NoAddress,
    SameAddress,
    NotLocated,
    NotLookedUp,
    TooLongForTheDay,
    TimeWindowConflict,
    NoRoomOnTour,
}
