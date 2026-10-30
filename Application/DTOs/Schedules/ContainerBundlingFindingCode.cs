// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The deterministic findings of the container bundling advice. New members are appended at the end.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContainerBundlingFindingCode>))]
public enum ContainerBundlingFindingCode
{
    NoServices,
    NoLocatableServices,
    BaseNotFound,
    TravelTimesNotCalculated,
    NothingFits,
    VisitsCannotBePlaced,
    ServicesFit,
    TravelTime,
    BreakFits,
    BreakDoesNotFit,
    TimeWindowConflicts,
    NoRoomOnTour,
    TooLongForTheDay,
    MissingAddress,
    SameAddress,
    MissingCoordinates,
    CoordinatesNotLookedUp,
}
