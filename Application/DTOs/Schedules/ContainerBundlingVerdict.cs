// Copyright (c) Heribert Gasparoli. SPDX-License-Identifier: AGPL-3.0-only

using System.Text.Json.Serialization;

namespace Klacks.Api.Application.DTOs.Schedules;

/// <summary>
/// The overall answer of the container bundling advice. New members are appended at the end.
/// </summary>
[JsonConverter(typeof(JsonStringEnumConverter<ContainerBundlingVerdict>))]
public enum ContainerBundlingVerdict
{
    NoServices,
    NoLocatableServices,
    BaseNotFound,
    TravelTimesNotCalculated,
    NothingFits,
    VisitsCannotBePlaced,
    SingleVisitOnly,
    PartlyFits,
    AllFit,
}
